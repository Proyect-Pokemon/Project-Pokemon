using Microsoft.AspNetCore.Mvc;
using ProjectPokemon.Models.Database;
using ProjectPokemon.Models.Database.Entities;
using ProjectPokemon.Models.Dtos.Nature;

namespace ProjectPokemon.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class NatureController : ControllerBase {
        private readonly UnitOfWork _unitOfWork;
        public NatureController(UnitOfWork unitOfWork) {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IEnumerable<GetAllNatureDto>> GetAllNature() {
            ICollection<Nature> natures = await _unitOfWork.NatureRepository.GetAllAsync();

            IEnumerable<GetAllNatureDto> getAllNatureDto = natures.Select(Nature => new GetAllNatureDto {
                Id = Nature.Id,
                Name = Nature.Name,
                StatBoost = Nature.StatBoost,
                StatDrop = Nature.StatDrop
            });
            return getAllNatureDto;
        }
    }
}
