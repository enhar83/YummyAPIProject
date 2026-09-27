using AutoMapper;
using Yummy.Core.DTOs.IngredientDTOs;
using Yummy.Entity;
using Yummy.Entity.Enums;

namespace Yummy.Business.Mappings
{
    public class IngredientMapping : Profile
    {
        public IngredientMapping()
        {
            CreateMap<Ingredient, IngredientListDto>()
                .ForMember(dest => dest.UnitName, opt => opt.MapFrom(src =>
                    src.Unit == IngredientUnit.Gram ? "g" :
                    src.Unit == IngredientUnit.Kilogram ? "kg" :
                    src.Unit == IngredientUnit.Milliliter ? "ml" :
                    src.Unit == IngredientUnit.Liter ? "L" :
                    src.Unit == IngredientUnit.Piece ? "adet" : "Bilinmiyor"));

            CreateMap<StockMovement, StockMovementListDto>()
                .ForMember(dest => dest.TypeName, opt => opt.MapFrom(src =>
                    src.Type == StockMovementType.StockIn ? "Stok Girişi" :
                    src.Type == StockMovementType.Waste ? "Fire" :
                    src.Type == StockMovementType.CountCorrection ? "Sayım Düzeltmesi" : "Bilinmiyor"))
                .ForMember(dest => dest.PerformedBy, opt => opt.MapFrom(src =>
                    src.PerformedByUser != null ? src.PerformedByUser.Name + " " + src.PerformedByUser.Surname : null));
        }
    }
}
