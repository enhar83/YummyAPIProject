namespace Yummy.Entity.Enums
{
    // şefin malzeme talebinin durumu. sadece Pending durumundaki talep değiştirilebilir:
    // Pending → Supplied (çalışan tedarik etti) | Rejected (çalışan reddetti) | Cancelled (şef iptal etti veya şef profili silindi).
    public enum IngredientRequestStatus
    {
        Pending = 1,
        Supplied = 2,
        Rejected = 3,
        Cancelled = 4
    }
}
