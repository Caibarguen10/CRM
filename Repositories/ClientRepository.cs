using CrmService.Data;
using CrmService.Domain;
using CrmService.DTOs;
using CrmService.Common;
using Microsoft.EntityFrameworkCore;

namespace CrmService.Repositories;

/// <summary>
/// Implementación del repositorio de clientes.
/// Proporciona acceso a datos de clientes con Entity Framework Core.
/// </summary>
/// <remarks>
/// PATRÓN REPOSITORY IMPLEMENTADO:
/// 
/// Esta clase encapsula toda la lógica de acceso a datos para clientes:
/// - Uso de DbContext para queries
/// - Aplicación de filtros dinámicos
/// - Paginación eficiente
/// - Optimizaciones de rendimiento (AsNoTracking)
/// 
/// OPTIMIZACIONES APLICADAS:
/// 
/// 1. AsNoTracking():
///    - Se usa en consultas de solo lectura (GetAll, GetById, GetByDocument)
///    - EF Core no rastrea los cambios de las entidades
///    - Mejora significativa en rendimiento y memoria
///    - NO usar en Update (se necesita tracking)
/// 
/// 2. Proyecciones y paginación:
///    - Skip/Take para paginación eficiente
///    - CountAsync() separado para el total
///    - OrderBy para resultados predecibles
/// 
/// 3. Queries asíncronas:
///    - Todos los métodos son async/await
///    - No bloquea threads durante I/O de base de datos
/// </remarks>
public class ClientRepository : IClientRepository
{
	/// <summary>
	/// Contexto de base de datos de Entity Framework Core.
	/// </summary>
	private readonly AppDbContext _context;

	/// <summary>
	/// Constructor del repositorio de clientes.
	/// </summary>
	/// <param name="context">Contexto de base de datos inyectado por DI</param>
	public ClientRepository(AppDbContext context)
	{
		_context = context;
	}

	/// <summary>
	/// Obtiene una lista paginada de clientes con filtros opcionales.
	/// </summary>
	/// <param name="pagination">Parámetros de paginación (página y tamaño)</param>
	/// <param name="filters">Filtros opcionales de búsqueda</param>
	/// <returns>Resultado paginado con clientes y metadatos</returns>
	/// <remarks>
	/// PROCESO DE EJECUCIÓN:
	/// 
	/// 1. INICIALIZACIÓN:
	///    - Crea query base: _context.Clients
	///    - Aplica AsNoTracking() para mejor rendimiento
	///    - El filtro global IsDeleted=false se aplica automáticamente
	/// 
	/// 2. APLICACIÓN DE FILTROS:
	///    - Si filters != null, aplica filtros dinámicos
	///    - Usa Contains() para búsquedas parciales (LIKE en SQL)
	///    - Los filtros son acumulativos (AND entre ellos)
	/// 
	/// 3. CONTEO TOTAL:
	///    - CountAsync() obtiene el total ANTES de paginar
	///    - Necesario para calcular TotalPages en el frontend
	///    - Se ejecuta como una query separada
	/// 
	/// 4. PAGINACIÓN:
	///    - OrderBy: Ordena por Id para resultados consistentes
	///    - Skip: Salta (página - 1) * tamaño registros
	///    - Take: Toma solo 'tamaño' registros
	///    - ToListAsync: Ejecuta la query y materializa los resultados
	/// 
	/// EJEMPLO SQL GENERADO:
	/// SELECT * FROM Clients
	/// WHERE IsDeleted = 0 
	///   AND FullName LIKE '%Juan%'
	/// ORDER BY Id
	/// OFFSET 10 ROWS FETCH NEXT 10 ROWS ONLY
	/// 
	/// RENDIMIENTO:
	/// - Solo trae los registros de la página actual
	/// - AsNoTracking() reduce overhead de EF Core
	/// - Índices en columnas filtradas mejoran búsquedas
	/// </remarks>
	public async Task<PagedResult<Client>> GetAllAsync(PaginationParams pagination, ClientFilterDto? filters = null)
	{
		// Inicializar query base con AsNoTracking para mejor rendimiento
		var query = _context.Clients.AsNoTracking();

		// ================================================================
		// APLICAR FILTROS DINÁMICOS
		// ================================================================
		// Los filtros se aplican solo si el parámetro correspondiente tiene valor
		// Esto permite búsquedas flexibles: buscar solo por documento, solo por nombre, etc.
		
		if (filters != null)
		{
			// Filtro por número de documento (búsqueda parcial)
			// Ejemplo: "123" encontrará "12345678", "00012300", etc.
			if (!string.IsNullOrWhiteSpace(filters.DocumentNumber))
			{
				query = query.Where(x => x.DocumentNumber.Contains(filters.DocumentNumber));
			}

			// Filtro por nombre completo (búsqueda parcial, case-insensitive en SQL)
			// Ejemplo: "juan" encontrará "Juan Pérez", "María Juana", etc.
			if (!string.IsNullOrWhiteSpace(filters.FullName))
			{
				query = query.Where(x => x.FullName.Contains(filters.FullName));
			}

			// Filtro por email (búsqueda parcial)
			// Ejemplo: "gmail" encontrará todos los emails de gmail
			if (!string.IsNullOrWhiteSpace(filters.Email))
			{
				query = query.Where(x => x.Email.Contains(filters.Email));
			}
		}

		// ================================================================
		// OBTENER TOTAL DE REGISTROS
		// ================================================================
		// Se ejecuta ANTES de aplicar Skip/Take para saber cuántos registros hay en total
		// Esto es necesario para calcular el número de páginas en el frontend
		var totalCount = await query.CountAsync();

		// ================================================================
		// APLICAR PAGINACIÓN Y OBTENER RESULTADOS
		// ================================================================
		var items = await query
			.OrderBy(x => x.Id)  // Ordenar para resultados consistentes
			.Skip((pagination.Page - 1) * pagination.PageSize)  // Saltar páginas anteriores
			.Take(pagination.PageSize)  // Tomar solo los registros de esta página
			.ToListAsync();  // Ejecutar la query y materializar los resultados

		// Construir y retornar el resultado paginado
		return new PagedResult<Client>
		{
			Items = items,              // Lista de clientes de la página actual
			TotalCount = totalCount,    // Total de registros (sin paginar)
			Page = pagination.Page,     // Página actual
			PageSize = pagination.PageSize  // Tamaño de página
			// TotalPages se calcula automáticamente en PagedResult
		};
	}

	/// <summary>
	/// Obtiene un cliente por su ID.
	/// </summary>
	/// <param name="id">ID del cliente</param>
	/// <returns>Cliente encontrado o null</returns>
	/// <remarks>
	/// OPTIMIZACIÓN:
	/// - Usa AsNoTracking() porque es solo lectura
	/// - FirstOrDefaultAsync() es más eficiente que Find() para queries con filtros
	/// 
	/// FILTRO AUTOMÁTICO:
	/// Solo retorna clientes con IsDeleted = false (filtro global activo)
	/// </remarks>
	public async Task<Client?> GetByIdAsync(int id)
	{
		return await _context.Clients
			.AsNoTracking()  // No rastrear cambios (solo lectura)
			.FirstOrDefaultAsync(x => x.Id == id);
	}

	/// <summary>
	/// Obtiene un cliente por su número de documento.
	/// </summary>
	/// <param name="documentNumber">Número de documento único</param>
	/// <returns>Cliente encontrado o null</returns>
	/// <remarks>
	/// USOS TÍPICOS:
	/// 1. Validar que no exista duplicado antes de crear
	/// 2. Búsqueda alternativa por documento en vez de ID
	/// 
	/// RENDIMIENTO:
	/// - DocumentNumber tiene índice único en la base de datos
	/// - La búsqueda es muy rápida (O(log n) con índice B-tree)
	/// </remarks>
	public async Task<Client?> GetByDocumentAsync(string documentNumber)
	{
		return await _context.Clients
			.AsNoTracking()  // No rastrear cambios (solo lectura)
			.FirstOrDefaultAsync(x => x.DocumentNumber == documentNumber);
	}

	/// <summary>
	/// Crea un nuevo cliente en la base de datos.
	/// </summary>
	/// <param name="client">Entidad cliente a crear</param>
	/// <returns>Cliente creado con ID generado</returns>
	/// <remarks>
	/// PROCESO AUTOMÁTICO AL GUARDAR:
	/// 
	/// 1. EF Core agrega la entidad al contexto (estado: Added)
	/// 2. SaveChangesAsync() detecta el estado Added
	/// 3. AppDbContext.ProcessAuditFields() se ejecuta automáticamente:
	///    - CreatedAt = DateTime.UtcNow
	///    - CreatedBy = usuario del JWT token
	///    - IsDeleted = false
	/// 4. Se ejecuta INSERT en la base de datos
	/// 5. La base de datos genera el ID (IDENTITY/AUTOINCREMENT)
	/// 6. EF Core actualiza el objeto client con el ID generado
	/// 
	/// IMPORTANTE:
	/// Después de SaveChangesAsync(), client.Id contendrá el ID generado.
	/// </remarks>
	public async Task<Client> CreateAsync(Client client)
	{
		// Agregar la entidad al contexto (estado: Added)
		_context.Clients.Add(client);
		
		// Guardar cambios (ejecuta INSERT)
		// Los campos de auditoría se llenan automáticamente
		await _context.SaveChangesAsync();
		
		// Retornar el cliente con el ID ya asignado
		return client;
	}

	/// <summary>
	/// Actualiza un cliente existente.
	/// </summary>
	/// <param name="client">Cliente con datos actualizados (debe incluir el Id)</param>
	/// <returns>Cliente actualizado o null si no existe</returns>
	/// <remarks>
	/// PROCESO DE ACTUALIZACIÓN:
	/// 
	/// 1. FindAsync() busca el cliente por ID (con tracking)
	/// 2. Si no existe, retorna null
	/// 3. Se actualizan solo las propiedades modificables
	/// 4. SaveChangesAsync() detecta los cambios (change tracking)
	/// 5. AppDbContext.ProcessAuditFields() se ejecuta automáticamente:
	///    - UpdatedAt = DateTime.UtcNow
	///    - UpdatedBy = usuario del JWT token
	/// 6. Se ejecuta UPDATE solo de las columnas modificadas
	/// 
	/// CAMPOS NO ACTUALIZABLES:
	/// - Id: Es la clave primaria, no se puede cambiar
	/// - CreatedAt, CreatedBy: Solo se asignan en creación
	/// - IsDeleted, DeletedAt, DeletedBy: Solo para soft delete
	/// 
	/// OPTIMIZACIÓN:
	/// EF Core solo actualiza las columnas que realmente cambiaron.
	/// Si ninguna propiedad cambió, no ejecuta UPDATE.
	/// </remarks>
	public async Task<Client?> UpdateAsync(Client client)
	{
		// Buscar el cliente existente (CON tracking para detectar cambios)
		var existing = await _context.Clients.FindAsync(client.Id);
		if (existing == null) return null;

		// Actualizar solo las propiedades modificables
		// Las propiedades de auditoría se actualizan automáticamente en SaveChanges
		existing.DocumentNumber = client.DocumentNumber;
		existing.FullName = client.FullName;
		existing.Email = client.Email;
		existing.Phone = client.Phone;

		// Guardar cambios (ejecuta UPDATE)
		// UpdatedAt y UpdatedBy se asignan automáticamente
		await _context.SaveChangesAsync();
		
		return existing;
	}

	/// <summary>
	/// Elimina un cliente (soft delete).
	/// </summary>
	/// <param name="id">ID del cliente a eliminar</param>
	/// <returns>true si se eliminó, false si no existe</returns>
	/// <remarks>
	/// SOFT DELETE AUTOMÁTICO:
	/// 
	/// Aunque este método usa Remove(), NO se elimina físicamente.
	/// El AppDbContext intercepta la eliminación y la convierte en soft delete:
	/// 
	/// 1. FindAsync() obtiene el cliente (con tracking)
	/// 2. Remove() marca la entidad para eliminación (estado: Deleted)
	/// 3. SaveChangesAsync() ejecuta ProcessAuditFields()
	/// 4. ProcessAuditFields() detecta estado Deleted y:
	///    - Cambia el estado a Modified (en lugar de Deleted)
	///    - Asigna IsDeleted = true
	///    - Asigna DeletedAt = DateTime.UtcNow
	///    - Asigna DeletedBy = usuario del JWT token
	/// 5. Se ejecuta UPDATE (no DELETE) en la base de datos
	/// 
	/// RESULTADO:
	/// El registro permanece en la base de datos pero:
	/// - IsDeleted = true
	/// - No aparece en consultas normales (filtro global activo)
	/// - Se puede restaurar cambiando IsDeleted a false
	/// 
	/// RELACIONES:
	/// Las entidades relacionadas (Contacts, Notes, Opportunities) NO se eliminan.
	/// Si se necesita eliminarlas también, debe hacerse manualmente antes.
	/// </remarks>
	public async Task<bool> DeleteAsync(int id)
	{
		// Buscar el cliente (CON tracking)
		var client = await _context.Clients.FindAsync(id);
		if (client == null) return false;

		// Marcar para eliminación
		// Nota: NO se elimina físicamente, se convierte en soft delete automáticamente
		_context.Clients.Remove(client);
		
		// Guardar cambios (ejecuta UPDATE, no DELETE)
		// IsDeleted, DeletedAt y DeletedBy se asignan automáticamente
		await _context.SaveChangesAsync();
		
		return true;
	}
}
