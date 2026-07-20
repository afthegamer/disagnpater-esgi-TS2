namespace RestaurantApi.Configuration;

public sealed record OpeningHours(TimeOnly Ouverture, TimeOnly Fermeture)
{
    public static OpeningHours ParDefaut() => new(new TimeOnly(11, 30), new TimeOnly(23, 0));

    public bool EstOuvert(TimeOnly heure) => Ouverture <= Fermeture
        ? heure >= Ouverture && heure < Fermeture
        : heure >= Ouverture || heure < Fermeture;

    public override string ToString() => $"{Ouverture:HH\\:mm} - {Fermeture:HH\\:mm}";
}
