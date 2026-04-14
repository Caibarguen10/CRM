namespace CrmService.Common;

/// <summary>
/// Clase genérica para estandarizar las respuestas de la API.
/// Envuelve todos los resultados (éxito o error) en un formato consistente.
/// </summary>
/// <typeparam name="T">Tipo de datos que contiene la respuesta</typeparam>
/// <remarks>
/// VENTAJAS de usar ApiResponse:
/// 
/// 1. CONSISTENCIA: Todas las respuestas tienen la misma estructura
/// 2. CLARIDAD: El frontend siempre sabe qué esperar
/// 3. MANEJO DE ERRORES: Incluye mensaje de error descriptivo
/// 4. TIPADO FUERTE: El compilador valida el tipo de datos
/// 
/// ESTRUCTURA JSON de respuesta:
/// {
///   "success": true/false,
///   "message": "Descripción de lo que ocurrió",
///   "data": { ... }  // Puede ser null en caso de error
/// }
/// 
/// EJEMPLOS DE USO:
/// 
/// Éxito con datos:
/// return Ok(ApiResponse&lt;ClientDto&gt;.Ok(client, "Cliente creado exitosamente"));
/// 
/// Error de validación:
/// return BadRequest(ApiResponse&lt;object&gt;.Fail("El documento ya existe"));
/// 
/// Lista de elementos:
/// return Ok(ApiResponse&lt;List&lt;ClientDto&gt;&gt;.Ok(clients, "Clientes obtenidos"));
/// </remarks>
public class ApiResponse<T>
{
	/// <summary>
	/// Indica si la operación fue exitosa.
	/// true = éxito, false = error.
	/// </summary>
	public bool Success { get; set; }
	
	/// <summary>
	/// Mensaje descriptivo del resultado de la operación.
	/// En caso de éxito: mensaje de confirmación.
	/// En caso de error: descripción del problema.
	/// </summary>
	public string Message { get; set; } = string.Empty;
	
	/// <summary>
	/// Datos de la respuesta (puede ser null en caso de error).
	/// Tipo genérico T permite usar cualquier tipo: DTOs, listas, primitivos, etc.
	/// </summary>
	public T? Data { get; set; }

	/// <summary>
	/// Método estático para crear una respuesta exitosa.
	/// </summary>
	/// <param name="data">Datos a retornar</param>
	/// <param name="message">Mensaje de éxito (opcional)</param>
	/// <returns>ApiResponse con Success=true y los datos proporcionados</returns>
	/// <example>
	/// var response = ApiResponse&lt;ClientDto&gt;.Ok(clientDto, "Cliente creado exitosamente");
	/// </example>
	public static ApiResponse<T> Ok(T data, string message = "Proceso exitoso")
	{
		return new ApiResponse<T>
		{
			Success = true,
			Message = message,
			Data = data
		};
	}

	/// <summary>
	/// Método estático para crear una respuesta de error.
	/// </summary>
	/// <param name="message">Mensaje de error descriptivo</param>
	/// <returns>ApiResponse con Success=false y Data=null</returns>
	/// <example>
	/// var response = ApiResponse&lt;object&gt;.Fail("El cliente no existe");
	/// </example>
	public static ApiResponse<T> Fail(string message)
	{
		return new ApiResponse<T>
		{
			Success = false,
			Message = message,
			Data = default  // null para tipos de referencia, valor por defecto para tipos de valor
		};
	}
	
	/// <summary>
	/// Método alternativo para crear una respuesta exitosa.
	/// Alias de Ok() para mayor claridad en el código.
	/// </summary>
	/// <param name="data">Datos a retornar</param>
	/// <param name="message">Mensaje de éxito (opcional)</param>
	/// <returns>ApiResponse con Success=true y los datos proporcionados</returns>
	public static ApiResponse<T> SuccessResponse(T data, string message = "Proceso exitoso")
	{
		return Ok(data, message);
	}
	
	/// <summary>
	/// Método alternativo para crear una respuesta de error.
	/// Alias de Fail() para mayor claridad en el código.
	/// </summary>
	/// <param name="message">Mensaje de error descriptivo</param>
	/// <returns>ApiResponse con Success=false y Data=null</returns>
	public static ApiResponse<T> ErrorResponse(string message)
	{
		return Fail(message);
	}
}
