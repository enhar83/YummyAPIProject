using AutoMapper;
using Yummy.Core.DTOs.IngredientDTOs;
using Yummy.Core.DTOs.IngredientRequestDTOs;
using Yummy.Entity;
using Yummy.Entity.Enums;

namespace Yummy.Business.Mappings
{
    public class IngredientMapping : Profile
    {
        public IngredientMapping()
        {
            CreateMap<Ingredient, IngredientListDto>()
                .ForMember(dest => dest.UnitName, opt => opt.MapFrom(src => GetUnitName(src.Unit)));

            CreateMap<StockMovement, StockMovementListDto>()
                .ForMember(dest => dest.TypeName, opt => opt.MapFrom(src =>
                    src.Type == StockMovementType.StockIn ? "Stok Girişi" :
                    src.Type == StockMovementType.Waste ? "Fire" :
                    src.Type == StockMovementType.CountCorrection ? "Sayım Düzeltmesi" :
                    src.Type == StockMovementType.RequestSupply ? "Talep Tedariki" : "Bilinmiyor"))
                .ForMember(dest => dest.PerformedBy, opt => opt.MapFrom(src =>
                    src.PerformedByUser != null ? src.PerformedByUser.Name + " " + src.PerformedByUser.Surname : null));

            CreateMap<IngredientRequest, IngredientRequestListDto>()
                .ForMember(dest => dest.StatusName, opt => opt.MapFrom(src =>
                    src.Status == IngredientRequestStatus.Pending ? "Bekliyor" :
                    src.Status == IngredientRequestStatus.Supplied ? "Tedarik Edildi" :
                    src.Status == IngredientRequestStatus.Rejected ? "Reddedildi" :
                    src.Status == IngredientRequestStatus.Cancelled ? "İptal Edildi" : "Bilinmiyor"))
                .ForMember(dest => dest.HandledBy, opt => opt.MapFrom(src =>
                    src.HandledByUser != null ? src.HandledByUser.Name + " " + src.HandledByUser.Surname : null))
                // satırlar malzeme adına göre sıralı listelenir (veritabanı satırları belirli bir sırayla döndürmez).
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items.OrderBy(i => i.IngredientName)));

            CreateMap<IngredientRequestItem, IngredientRequestItemListDto>()
                .ForMember(dest => dest.UnitName, opt => opt.MapFrom(src => GetUnitName(src.Unit)));
        }

        // birimlerin ekranda gösterimi (malzeme listesi ve talep satırları aynı kısaltmaları kullanır).
        private static string GetUnitName(IngredientUnit unit) => unit switch
        {
            IngredientUnit.Gram => "g",
            IngredientUnit.Kilogram => "kg",
            IngredientUnit.Milliliter => "ml",
            IngredientUnit.Liter => "L",
            IngredientUnit.Piece => "adet",
            _ => "Bilinmiyor"
        };
    }
}
