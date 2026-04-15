using CrmService.Domain;

namespace CrmService.Data;

/// <summary>
/// Clase responsable de inicializar la base de datos con datos de prueba.
/// Se ejecuta automáticamente al iniciar la aplicación en Program.cs.
/// </summary>
public static class DbSeeder
{
	/// <summary>
	/// Inicializa la base de datos con datos de ejemplo para desarrollo y testing.
	/// </summary>
	/// <param name="context">Contexto de base de datos de Entity Framework</param>
	/// <remarks>
	/// <strong>Datos que se crean:</strong>
	/// <list type="bullet">
	///   <item><strong>Usuario Admin</strong> - Usuario: admin@crm.com, Password: Admin123!</item>
	///   <item><strong>Usuario Asesor</strong> - Usuario: asesor@crm.com, Password: Asesor123!</item>
	///   <item><strong>Usuario Auditor</strong> - Usuario: auditor@crm.com, Password: Auditor123!</item>
	///   <item><strong>2 Clientes de ejemplo</strong> con contactos y notas</item>
	///   <item><strong>1 Oportunidad de negocio</strong> para el primer cliente</item>
	/// </list>
	/// 
	/// <strong>IMPORTANTE:</strong>
	/// - Solo se ejecuta si no existen usuarios (base de datos vacía)
	/// - Las contraseñas están hasheadas con BCrypt
	/// - Los campos de auditoría se establecen manualmente con "System"
	/// </remarks>
	public static void SeedData(AppDbContext context)
	{
		// Solo hacer seed si no hay usuarios (base de datos vacía)
		if (context.Users.Any())
		{
			Console.WriteLine("⚠️  La base de datos ya contiene datos. Seeding omitido.");
			return;
		}

		Console.WriteLine("🌱 Iniciando seeding de datos...");

		// ========================================
		// 1. CREAR USUARIOS CON DIFERENTES ROLES
		// ========================================
		
		var adminUser = new User
		{
			Username = "admin",
			Email = "admin@crm.com",
			// Password: "Admin123!" hasheada con BCrypt (factor de trabajo 12)
			PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!", 12),
			Role = UserRole.Admin,
			CreatedBy = "System",
			CreatedAt = DateTime.UtcNow
		};

		var asesorUser = new User
		{
			Username = "asesor",
			Email = "asesor@crm.com",
			// Password: "Asesor123!" hasheada con BCrypt
			PasswordHash = BCrypt.Net.BCrypt.HashPassword("Asesor123!", 12),
			Role = UserRole.Asesor,
			CreatedBy = "System",
			CreatedAt = DateTime.UtcNow
		};

		var auditorUser = new User
		{
			Username = "auditor",
			Email = "auditor@crm.com",
			// Password: "Auditor123!" hasheada con BCrypt
			PasswordHash = BCrypt.Net.BCrypt.HashPassword("Auditor123!", 12),
			Role = UserRole.Auditor,
			CreatedBy = "System",
			CreatedAt = DateTime.UtcNow
		};

		context.Users.AddRange(adminUser, asesorUser, auditorUser);
		context.SaveChanges();
		Console.WriteLine("✅ 3 usuarios creados (Admin, Asesor, Auditor)");

		// ========================================
		// 2. CREAR CLIENTES DE EJEMPLO
		// ========================================
		
		var client1 = new Client
		{
			DocumentNumber = "12345678",
			FullName = "Juan Pérez García",
			Email = "juan.perez@email.com",
			Phone = "+34 600 123 456",
			CreatedBy = "admin",
			CreatedAt = DateTime.UtcNow
		};

		var client2 = new Client
		{
			DocumentNumber = "87654321",
			FullName = "María López Fernández",
			Email = "maria.lopez@email.com",
			Phone = "+34 600 654 321",
			CreatedBy = "admin",
			CreatedAt = DateTime.UtcNow
		};

		context.Clients.AddRange(client1, client2);
		context.SaveChanges();
		Console.WriteLine("✅ 2 clientes creados");

		// ========================================
		// 3. CREAR CONTACTOS PARA LOS CLIENTES
		// ========================================
		
		var contact1 = new Contact
		{
			ClientId = client1.Id,
			Name = "Juan Pérez García",
			Email = "juan.perez@email.com",
			Phone = "+34 600 123 456",
			Position = "Director General",
			CreatedBy = "admin",
			CreatedAt = DateTime.UtcNow
		};

		var contact2 = new Contact
		{
			ClientId = client2.Id,
			Name = "María López Fernández",
			Email = "maria.lopez@email.com",
			Phone = "+34 600 654 321",
			Position = "Directora de Operaciones",
			CreatedBy = "admin",
			CreatedAt = DateTime.UtcNow
		};

		context.Contacts.AddRange(contact1, contact2);
		context.SaveChanges();
		Console.WriteLine("✅ 2 contactos creados");

		// ========================================
		// 4. CREAR NOTAS PARA LOS CLIENTES
		// ========================================
		
		var note1 = new ClientNote
		{
			ClientId = client1.Id,
			Note = "Primera reunión realizada. El cliente muestra interés en nuestros servicios de consultoría. Programar demo para la próxima semana.",
			CreatedBy = "asesor",
			CreatedAt = DateTime.UtcNow.AddDays(-2)
		};

		var note2 = new ClientNote
		{
			ClientId = client1.Id,
			Note = "Demo completada exitosamente. El cliente solicita propuesta económica detallada. Presupuesto estimado: 25,000 EUR.",
			CreatedBy = "asesor",
			CreatedAt = DateTime.UtcNow.AddDays(-1)
		};

		var note3 = new ClientNote
		{
			ClientId = client2.Id,
			Note = "Cliente referido por Juan Pérez. Interesada en servicios de auditoría. Coordinar llamada con el equipo técnico.",
			CreatedBy = "admin",
			CreatedAt = DateTime.UtcNow
		};

		context.ClientNotes.AddRange(note1, note2, note3);
		context.SaveChanges();
		Console.WriteLine("✅ 3 notas creadas");

		// ========================================
		// 5. CREAR OPORTUNIDADES DE NEGOCIO
		// ========================================
		
		var opportunity1 = new Opportunity
		{
			ClientId = client1.Id,
			Title = "Proyecto de Consultoría CRM",
			EstimatedAmount = 25000.00m,
			Status = "Proposal",
			CreatedBy = "asesor",
			CreatedAt = DateTime.UtcNow
		};

		var opportunity2 = new Opportunity
		{
			ClientId = client2.Id,
			Title = "Auditoría de Sistemas IT",
			EstimatedAmount = 15000.00m,
			Status = "Qualification",
			CreatedBy = "admin",
			CreatedAt = DateTime.UtcNow
		};

		context.Opportunities.AddRange(opportunity1, opportunity2);
		context.SaveChanges();
		Console.WriteLine("✅ 2 oportunidades de negocio creadas");

		Console.WriteLine("✅ Seeding completado exitosamente!");
		Console.WriteLine("");
		Console.WriteLine("📋 CREDENCIALES DE ACCESO:");
		Console.WriteLine("---------------------------");
		Console.WriteLine("👤 Admin:   admin@crm.com / Admin123!");
		Console.WriteLine("👤 Asesor:  asesor@crm.com / Asesor123!");
		Console.WriteLine("👤 Auditor: auditor@crm.com / Auditor123!");
		Console.WriteLine("");
	}
}
