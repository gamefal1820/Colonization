using System.Collections.Generic;

[System.Serializable]
public class CountryInformation
{
    public string Name;

    public int Population;
    public float Forces;
    public int Income;
    public float DeplomacyRate;

    public bool IsCaptured;
}

[System.Serializable]
public class CountryListWrapper
{
    public List<CountryInformation> Countries;
    public int Money;
}
