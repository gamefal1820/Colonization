using System.Collections.Generic;

[System.Serializable]
public class CountryInformation
{
    public string Name;

    public float Population;
    public int Forces;
    public float Income;
    public float DeplomacyRate;

    public bool IsCaptured;
}

[System.Serializable]
public class CountryListWrapper
{
    public List<CountryInformation> Countries;
    public int Money;
}
