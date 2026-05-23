using AutoMapper;
using Firmeza.Api.Dtos;
using Firmeza.Core.Entities;
using Firmeza.Core.Interfaces;
using Firmeza.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;

    public ClientsController(ApplicationDbContext context, IMapper mapper, IEmailService emailService)
    {
        _context = context;
        _mapper = mapper;
        _emailService = emailService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClientDto>>> GetClients()
    {
        var clients = await _context.Clients.ToListAsync();
        return Ok(_mapper.Map<IEnumerable<ClientDto>>(clients));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ClientDto>> GetClient(int id)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client == null)
            return NotFound("Cliente no encontrado.");

        return Ok(_mapper.Map<ClientDto>(client));
    }

    [HttpPost]
    public async Task<ActionResult<ClientDto>> CreateClient([FromBody] CreateClientDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Validar duplicado de documento
        if (await _context.Clients.AnyAsync(c => c.DocumentNumber == dto.DocumentNumber))
            return BadRequest("El número de documento ya está registrado.");

        // Validar duplicado de correo
        if (await _context.Clients.AnyAsync(c => c.Email == dto.Email))
            return BadRequest("El correo electrónico ya está registrado.");

        var client = _mapper.Map<Client>(dto);
        client.CreatedAt = DateTime.UtcNow;

        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        // Enviar correo de bienvenida
        try
        {
            await _emailService.SendEmailAsync(
                client.Email,
                "Registro Exitoso - Firmeza",
                $"<h1>Bienvenido a Firmeza</h1><p>Hola {client.FullName}, tu cuenta ha sido creada exitosamente.</p>"
            );
        }
        catch
        {
            // Ignoramos fallos de correo para no revertir la transacción en BD
        }

        var resultDto = _mapper.Map<ClientDto>(client);
        return CreatedAtAction(nameof(GetClient), new { id = client.Id }, resultDto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateClient(int id, [FromBody] UpdateClientDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var client = await _context.Clients.FindAsync(id);
        if (client == null)
            return NotFound("Cliente no encontrado.");

        // Validar duplicado de documento con otros clientes
        if (await _context.Clients.AnyAsync(c => c.DocumentNumber == dto.DocumentNumber && c.Id != id))
            return BadRequest("El número de documento ya está registrado por otro cliente.");

        // Validar duplicado de correo con otros clientes
        if (await _context.Clients.AnyAsync(c => c.Email == dto.Email && c.Id != id))
            return BadRequest("El correo electrónico ya está registrado por otro cliente.");

        _mapper.Map(dto, client);

        _context.Entry(client).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return Ok(_mapper.Map<ClientDto>(client));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteClient(int id)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client == null)
            return NotFound("Cliente no encontrado.");

        // Validar si tiene ventas asociadas
        var hasSales = await _context.Sales.AnyAsync(s => s.ClientId == id);
        if (hasSales)
            return BadRequest("No se puede eliminar el cliente porque tiene ventas registradas.");

        _context.Clients.Remove(client);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
