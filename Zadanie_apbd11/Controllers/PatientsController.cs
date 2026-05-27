using Zadanie_apbd11.Data;
using Zadanie_apbd11.DTOs;
using Zadanie_apbd11.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Zadanie_apbd11.Controllers;

[ApiController]
[Route("api/patients")]
public class PatientsController : ControllerBase
{
    private readonly HospitalDbContext _context;

    public PatientsController(HospitalDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetPatients([FromQuery] string? search)
    {
        var query = _context.Patients
            .Include(p => p.Admissions)
                .ThenInclude(a => a.Ward)
            .Include(p => p.BedAssignments)
                .ThenInclude(ba => ba.Bed)
                    .ThenInclude(b => b.BedType)
            .Include(p => p.BedAssignments)
                .ThenInclude(ba => ba.Bed)
                    .ThenInclude(b => b.Room)
                        .ThenInclude(r => r.Ward)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";

            query = query.Where(p =>
                EF.Functions.Like(p.FirstName, pattern) ||
                EF.Functions.Like(p.LastName, pattern));
        }

        var patients = await query
            .Select(p => new PatientDto
            {
                Pesel = p.Pesel,
                FirstName = p.FirstName,
                LastName = p.LastName,
                Age = p.Age,
                Sex = p.Sex ? "Male" : "Female",

                Admissions = p.Admissions.Select(a => new AdmissionDto
                {
                    Id = a.Id,
                    AdmissionDate = a.AdmissionDate,
                    DischargeDate = a.DischargeDate,
                    Ward = new WardDto
                    {
                        Id = a.Ward.Id,
                        Name = a.Ward.Name,
                        Description = a.Ward.Description
                    }
                }).ToList(),

                BedAssignments = p.BedAssignments.Select(ba => new BedAssignmentDto
                {
                    Id = ba.Id,
                    From = ba.From,
                    To = ba.To,
                    Bed = new BedDto
                    {
                        Id = ba.Bed.Id,
                        BedType = new BedTypeDto
                        {
                            Id = ba.Bed.BedType.Id,
                            Name = ba.Bed.BedType.Name,
                            Description = ba.Bed.BedType.Description
                        },
                        Room = new RoomDto
                        {
                            Id = ba.Bed.Room.Id,
                            HasTv = ba.Bed.Room.HasTv,
                            Ward = new WardDto
                            {
                                Id = ba.Bed.Room.Ward.Id,
                                Name = ba.Bed.Room.Ward.Name,
                                Description = ba.Bed.Room.Ward.Description
                            }
                        }
                    }
                }).ToList()
            })
            .ToListAsync();

        return Ok(patients);
    }

    [HttpPost("{pesel}/bedassignments")]
    public async Task<IActionResult> CreateBedAssignment(
        string pesel,
        [FromBody] CreateBedAssignmentRequest request)
    {
        if (request.From == default)
        {
            return BadRequest("Pole 'from' jest wymagane.");
        }

        if (request.To.HasValue && request.To.Value <= request.From)
        {
            return BadRequest("Pole 'to' musi być późniejsze niż pole 'from'.");
        }

        if (string.IsNullOrWhiteSpace(request.BedType))
        {
            return BadRequest("Pole 'bedType' jest wymagane.");
        }

        if (string.IsNullOrWhiteSpace(request.Ward))
        {
            return BadRequest("Pole 'ward' jest wymagane.");
        }

        var patientExists = await _context.Patients.AnyAsync(p => p.Pesel == pesel);

        if (!patientExists)
        {
            return NotFound($"Nie znaleziono pacjenta o numerze PESEL {pesel}.");
        }

        var bedTypeExists = await _context.BedTypes.AnyAsync(bt => bt.Name == request.BedType);

        if (!bedTypeExists)
        {
            return NotFound($"Nie znaleziono typu łóżka: {request.BedType}.");
        }

        var wardExists = await _context.Wards.AnyAsync(w => w.Name == request.Ward);

        if (!wardExists)
        {
            return NotFound($"Nie znaleziono oddziału: {request.Ward}.");
        }

        var requestFrom = request.From;
        var requestTo = request.To;

        var beds = await _context.Beds
            .Include(b => b.BedType)
            .Include(b => b.Room)
                .ThenInclude(r => r.Ward)
            .Include(b => b.BedAssignments)
            .Where(b =>
                b.BedType.Name == request.BedType &&
                b.Room.Ward.Name == request.Ward)
            .ToListAsync();

        var availableBed = beds.FirstOrDefault(b =>
            !b.BedAssignments.Any(ba =>
                ba.From < (requestTo ?? DateTime.MaxValue) &&
                (ba.To ?? DateTime.MaxValue) > requestFrom));

        if (availableBed == null)
        {
            return NotFound(
                $"Nie znaleziono wolnego łóżka typu '{request.BedType}' na oddziale '{request.Ward}' w podanym terminie.");
        }

        var bedAssignment = new BedAssignment
        {
            PatientPesel = pesel,
            BedId = availableBed.Id,
            From = request.From,
            To = request.To
        };

        _context.BedAssignments.Add(bedAssignment);
        await _context.SaveChangesAsync();

        var response = new
        {
            id = bedAssignment.Id,
            patientPesel = bedAssignment.PatientPesel,
            bedId = bedAssignment.BedId,
            from = bedAssignment.From,
            to = bedAssignment.To,
            message = "Pacjent został przypisany do łóżka."
        };

        return Created($"/api/patients/{pesel}/bedassignments/{bedAssignment.Id}", response);
    }
}