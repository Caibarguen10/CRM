using System.Net;
using System.Text.Json;
using CrmService.Common;

namespace CrmService.Middleware;

/// <summary>
/// Middleware de manejo centralizado de excepciones.
/// Intercepta todas las excepciones no controladas y las convierte en respuestas HTTP apropiadas.
/// </summary>
/// <remarks>
/// PROPÓSITO:
/// En lugar de que las excepciones generen un error 500 genérico,
/// este middleware las captura y devuelve respuestas estructuradas con:
/// - Código HTTP correcto según el tipo de error
/// - Mensaje descriptivo en formato JSON
/// - Estructura consistente usando ApiResponse
/// 
/// MAPEO DE EXCEPCIONES → CÓDIGOS HTTP:
/// 
/// KeyNotFoundException → 404 Not Found
/// - Se lanza cuando no se encuentra un recurso
/// - Ejemplo: "El cliente con ID 123 no existe"
/// 
/// InvalidOperationException → 400 Bad Request
/// - Se lanza cuando la operación no es válida
/// - Ejemplo: "El documento ya existe", "No se puede eliminar"
/// 
/// UnauthorizedAccessException → 401 Unauthorized
/// - Se lanza cuando las credenciales son inválidas
/// - Ejemplo: "Credenciales inválidas"
/// 
/// Exception (genérica) → 500 Internal Server Error
/// - Cualquier otra excepción no esperada
/// - Se devuelve un mensaje genérico por seguridad
/// - El detalle se loguea pero NO se expone al cliente
/// 
/// VENTAJAS:
/// 1. No es necesario try-catch en cada controller
/// 2. Respuestas consistentes en toda la API
/// 3. Separación de concerns: controllers solo lanzan excepciones
/// 4. Fácil agregar nuevos tipos de error
/// 5. Los errores 500 no exponen detalles de implementación
/// 
/// ORDEN EN EL PIPELINE:
/// Este middleware debe registrarse PRIMERO en Program.cs:
/// app.UseMiddleware&lt;ErrorHandlingMiddleware&gt;(); // PRIMERO
/// app.UseAuthentication();
/// app.UseAuthorization();
/// // ...otros middlewares
/// </remarks>
public class ErrorHandlingMiddleware
{
	/// <summary>
	/// Delegado para invocar el siguiente middleware en el pipeline.
	/// </summary>
	private readonly RequestDelegate _next;

	/// <summary>
	/// Constructor del middleware.
	/// </summary>
	/// <param name="next">Siguiente middleware en el pipeline de ASP.NET Core</param>
	public ErrorHandlingMiddleware(RequestDelegate next)
	{
		_next = next;
	}

	/// <summary>
	/// Método principal que se ejecuta en cada request HTTP.
	/// Envuelve el pipeline completo en un try-catch.
	/// </summary>
	/// <param name="context">Contexto HTTP de la petición actual</param>
	/// <returns>Task asíncrono</returns>
	/// <remarks>
	/// FLUJO:
	/// 1. Intenta ejecutar el siguiente middleware (y toda la cadena)
	/// 2. Si algún middleware/controller lanza una excepción:
	///    - La captura aquí
	///    - Determina el código HTTP apropiado
	///    - Formatea la respuesta JSON
	///    - Devuelve al cliente
	/// 3. Si no hay excepciones, la respuesta fluye normalmente
	/// </remarks>
	public async Task Invoke(HttpContext context)
	{
		try
		{
			// Ejecutar el siguiente middleware en la cadena
			// Si hay una excepción en cualquier punto, se captura aquí
			await _next(context);
		}
		catch (KeyNotFoundException ex)
		{
			// 404 Not Found: El recurso solicitado no existe
			// Ejemplo: Cliente no encontrado, Usuario no existe
			await HandleException(context, HttpStatusCode.NotFound, ex.Message);
		}
		catch (InvalidOperationException ex)
		{
			// 400 Bad Request: La operación no es válida o permitida
			// Ejemplo: Documento duplicado, Estado inválido, Regla de negocio violada
			await HandleException(context, HttpStatusCode.BadRequest, ex.Message);
		}
		catch (UnauthorizedAccessException ex)
		{
			// 401 Unauthorized: Credenciales inválidas o falta autenticación
			// Ejemplo: Login fallido, Token inválido
			await HandleException(context, HttpStatusCode.Unauthorized, ex.Message);
		}
		catch (Exception ex)
		{
			// 500 Internal Server Error: Error inesperado
			// IMPORTANTE: NO exponemos el mensaje real por seguridad
			// El detalle se debería loguear en un sistema de logs
			
			// TODO: Agregar logging del error real aquí
			// _logger.LogError(ex, "Error interno no controlado");
			
			await HandleException(
				context, 
				HttpStatusCode.InternalServerError, 
				"Ocurrió un error interno."  // Mensaje genérico por seguridad
			);
		}
	}

	/// <summary>
	/// Maneja la excepción generando una respuesta HTTP estructurada.
	/// </summary>
	/// <param name="context">Contexto HTTP</param>
	/// <param name="statusCode">Código de estado HTTP a devolver</param>
	/// <param name="message">Mensaje de error para el cliente</param>
	/// <returns>Task asíncrono</returns>
	/// <remarks>
	/// PROCESO:
	/// 1. Establece Content-Type como application/json
	/// 2. Establece el código de estado HTTP
	/// 3. Crea un ApiResponse con Success=false
	/// 4. Serializa a JSON
	/// 5. Escribe la respuesta
	/// 
	/// RESPUESTA JSON generada:
	/// {
	///   "success": false,
	///   "message": "Descripción del error",
	///   "data": null
	/// }
	/// </remarks>
	private static async Task HandleException(HttpContext context, HttpStatusCode statusCode, string message)
	{
		// Establecer el tipo de contenido de la respuesta
		context.Response.ContentType = "application/json";
		
		// Establecer el código de estado HTTP
		context.Response.StatusCode = (int)statusCode;

		// Crear la respuesta estructurada
		var response = ApiResponse<string>.Fail(message);
		
		// Serializar a JSON
		// Opciones por defecto: camelCase, sin formato, etc.
		var json = JsonSerializer.Serialize(response);

		// Escribir la respuesta al cliente
		await context.Response.WriteAsync(json);
	}
}
