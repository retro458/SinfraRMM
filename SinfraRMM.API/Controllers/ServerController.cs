using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SinfraRMM.API.Dtos;
using SinfraRMM.API.Interfaces;

namespace SinfraRMM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Todos los endpoints requieren sesión
    public class ServersController : ControllerBase
    {
        private readonly IServerService _serverService;

        public ServersController(IServerService serverService)
        {
            _serverService = serverService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var servers = await _serverService.GetAllAsync();
            return Ok(RespuestaDto.Ok("Servidores obtenidos.", servers));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var server = await _serverService.GetByIdAsync(id);
                return Ok(RespuestaDto.Ok("Servidor encontrado.", server));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(RespuestaDto.Error(ex.Message));
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]  // Solo Admin puede registrar servidores
        public async Task<IActionResult> Create([FromBody] CreateServerDto dto)
        {
            var server = await _serverService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById),
                new { id = server.Id },
                RespuestaDto.Ok("Servidor registrado.", server));
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,Tecnico")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServerDto dto)
        {
            try
            {
                var server = await _serverService.UpdateAsync(id, dto);
                return Ok(RespuestaDto.Ok("Servidor actualizado.", server));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(RespuestaDto.Error(ex.Message));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(RespuestaDto.Error(ex.Message));
            }
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _serverService.DeleteAsync(id);
                return Ok(RespuestaDto.Ok("Servidor eliminado."));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(RespuestaDto.Error(ex.Message));
            }
        }

        // Endpoint especial para rotar el ApiKey si se compromete
        [HttpPost("{id:guid}/regenerate-key")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RegenerateKey(Guid id)
        {
            try
            {
                var newKey = await _serverService.RegenerateApiKeyAsync(id);
                return Ok(RespuestaDto.Ok("ApiKey regenerada.", new { apiKey = newKey }));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(RespuestaDto.Error(ex.Message));
            }
        }
    }
}