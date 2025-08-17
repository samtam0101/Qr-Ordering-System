namespace Infrastructure.Models
{
    public enum OrderStatus
    {
        // canonical values
        Draft = 0,         // cart, not yet placed (hidden from KDS/Admin)
        Submitted = 1,     // guest placed order (visible to KDS/Admin)
        InProgress = 2,    // kitchen started
        Ready = 3,         // ready to serve
        Served = 4,        // delivered
        Cancelled = 5,

        // ---- compatibility aliases (do NOT change numeric values) ----
        New = Draft,       // legacy code used "New"
        Pending = Draft,   // legacy code used "Pending"
        Placed = Submitted // legacy code used "Placed"
    }
}
