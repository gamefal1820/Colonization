using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public TextAsset NewgamejsonFile;

    float money;

    public float Money { get { return money; } set { money = Mathf.Round(value * 100) / 100; uiMoney.text = money + "$"; } }

    public bool Startgame;

    public List<CountryInformation> Countries;

    public static IReadOnlyList<float> InitCountriesIncome;

    public static IReadOnlyList<float> InitCountriesPopulation;

    void Awake()
    {
        Instance = this;


        //load the game as a new game
        var data = JsonUtility.FromJson<CountryListWrapper>(NewgamejsonFile.text);
        Countries = data.Countries;
        Money = data.Money;

        InitCountriesIncome = Countries.Select(c => c.Income).ToList().AsReadOnly();
        InitCountriesPopulation = Countries.Select(c => c.Population).ToList().AsReadOnly();
        randomizer = Random.Range(-10, 11);

    }
    void Update()
    {
        if (Startgame)
        {
            TheGameProcess();
        }
    }


    float timerForMoney;
    float timerForEvents;
    int randomizer;
    public void TheGameProcess()
    {

        //Giving Income
        timerForMoney += Time.deltaTime;
        timerForEvents += Time.deltaTime;
        if (timerForMoney >= 1)
        {
            foreach (var item in Countries.FindAll(_ => _.IsCaptured))
            {
                Money += item.Income;
            }
            timerForMoney = 0;
        }
        if (timerForEvents >= 45 + randomizer)
        {
            timerForEvents = 0;
            randomizer = Random.Range(-10, 11);
            StartRandomEvent();
        }

    }

    public void StartRandomEvent()
    {
        CountryInformation Doer;
        do
        {
            Doer = Countries[Random.Range(0, Countries.Count)];
        } while (Doer.IsCaptured);

        switch (Random.Range(0, 1))
        {
            case 0:
                AttackEvent(Doer);
                break;
            default:
                break;
        }
        CountryPanelRefresh();

    }

    public void CountryState(CountryInformation target, bool captureState)
    {
        if (captureState)
        {
            GameObject.Find(target.Name).GetComponent<SpriteRenderer>().color = Color.Lerp(Color.blue, Color.green, 0.75f);
            target.IsCaptured = true;
        }
        else
        {
            GameObject.Find(target.Name).GetComponent<SpriteRenderer>().color = Color.Lerp(Color.white, Color.white, 0);
            target.IsCaptured = false;
        }

    }

    public void AttackEvent(CountryInformation attacker)
    {
        List<CountryInformation> possibleTargets = new List<CountryInformation>();
        List<int> weights = new List<int>();
        foreach (var c in Countries)
        {
            if (c == attacker) continue;

            int score = 0; // حداقل شانس
            //در این بخش برای وزنگیری شانس، اختلاف را ضربدر ضریب میکنیم تا شدت  را بدست آوریم
            score += Mathf.FloorToInt(4 * (attacker.Forces - c.Forces));
            score += Mathf.FloorToInt(2 * (10 * (attacker.DeplomacyRate - c.DeplomacyRate)));
            score += Mathf.FloorToInt(1 * (attacker.Income - c.Income));

            if (score < 0)
            {
                score = Mathf.RoundToInt(score * 0.45f);
            }

            possibleTargets.Add(c);
            weights.Add(score);
        }

        {
            // اندیس‌های مقادیر منفی
            var negIdx = Enumerable.Range(0, weights.Count)
                                   .Where(i => weights[i] < 0)
                                   .ToList();

            if (negIdx.Count >= 2)
            {
                // مرتب بر اساس مقدار برای قرینه‌سازی کوچک↔بزرگ
                negIdx.Sort((i, j) => weights[i].CompareTo(weights[j]));

                int left = 0, right = negIdx.Count - 1;
                while (left < right)
                {

                    int i = negIdx[left];
                    int j = negIdx[right];

                    // swap در آرایه‌ی اصلی
                    int tmp = weights[i];
                    weights[i] = weights[j];
                    weights[j] = tmp;

                    left++;
                    right--;
                }
            }


        }


        // انتخاب بر اساس وزن
        int totalWeight = 0;
        int maxNegativeValue = Mathf.Abs(weights.Min());
        for (int i = 0; i < weights.Count; i++)
        {
            weights[i] += maxNegativeValue;
            totalWeight += weights[i];
            print("#" + weights[i] + " " + possibleTargets[i].Name);
        }


        int randomValue = Random.Range(0, totalWeight + 1);
        int cumulative = 0;

        for (int i = 0; i < possibleTargets.Count; i++)
        {
            cumulative += weights[i];
            if (randomValue <= cumulative)
            {
                var target = possibleTargets[i];
                AttackStart(target, attacker);
                print("cumulative: " + cumulative);
                return;
            }

        }
    }

    //UI section

    [SerializeField] TMP_Text uiMoney;

    [SerializeField] TMP_Text uiName;
    [SerializeField] TMP_Text uiPopulation;
    [SerializeField] TMP_Text uiForces;
    [SerializeField] TMP_Text uiIncome;
    [SerializeField] Button uiAttackButton;
    [SerializeField] Slider uiDiplomacyRate;
    [SerializeField] GameObject uiBackground;
    [SerializeField] Animator countryPanelAnimator;
    [SerializeField] Animator newsAnimator;
    [SerializeField] Animator CountrySelectorAnimator;


    public void CountryPanelSet(CountryInformation info)
    {
        uiName.text = info.Name;
        uiPopulation.text = "Population: " + info.Population + "M";
        uiForces.text = "Forces: " + info.Forces + "K";
        uiIncome.text = "Income: +" + info.Income + "K";
        uiDiplomacyRate.value = info.DeplomacyRate;
        if (info.IsCaptured) uiAttackButton.interactable = false;
        else uiAttackButton.interactable = true;
        //CountryPanel Animation
        countryPanelAnimator.SetTrigger("Open");
    }
    public void CountryPanelRefresh() { if (countryPanelAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name == "PanelOpen") CountryPanelSet(Countries.Find(_ => _.Name == uiName.text)); }
    public void NewsSet()
    {
        var _clipInfo = newsAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
        if (_clipInfo == "NewsOpen")
        {
            newsAnimator.SetTrigger("Close");
        }
        else if (_clipInfo == "NewsClose")
        {
            newsAnimator.SetTrigger("Open");
        }
    }

    [SerializeField] Button CountrySelectorOkButton;     // دکمه تایید
    [SerializeField] Button CountrySelectorCancelButton; // دکمه لغو
    [SerializeField] TMP_Dropdown CountrySelectorDropDown;
    [SerializeField] TMP_InputField CountrySelectorInput;


    //این تابع از ما متغیری که قرار هست از کشور مقایسه شود بعد بودن یا نبودن دکمه مقدار را میگیرد و در خروجی کشور و مقدار تحویل میدهد
    IEnumerator CountrySelectorSet(string variable, bool isAmount, System.Action<CountryInformation, int> onResult)
    {
        uiBackground.SetActive(true);
        if (isAmount) CountrySelectorInput.gameObject.SetActive(true);
        else CountrySelectorInput.gameObject.SetActive(false);
        //set dropdown
        {
            CountrySelectorDropDown.ClearOptions();
            List<TMP_Dropdown.OptionData> optionDataList = new List<TMP_Dropdown.OptionData>();
            int selectMatch = -1;
            foreach (var item in Countries.FindAll(_ => _.IsCaptured))
            {
                TMP_Dropdown.OptionData optionData = new TMP_Dropdown.OptionData();
                optionData.text = item.Name;
                optionDataList.Add(optionData);
                if (item.Name == uiName.text) selectMatch = optionDataList.Count - 1;
            }
            CountrySelectorDropDown.AddOptions(optionDataList);
            if (selectMatch != -1) CountrySelectorDropDown.value = selectMatch; CountrySelectorDropDown.RefreshShownValue();
        }


        CountrySelectorAnimator.SetTrigger("Open");

        bool result = false;
        bool finished = false;

        // گوش دادن به دکمه‌ها
        CountrySelectorOkButton.onClick.AddListener(() =>
        {
            if (isAmount && CountrySelectorInput.text != "")
            {
                // نام متغیر موردنظر مثلا population یا forces
                FieldInfo maxLimitForAccept = typeof(CountryInformation).GetField(variable);
                print(variable);

                if (variable != "")
                {
                    if (float.Parse(maxLimitForAccept.GetValue(Countries.Find(_ => _ == Countries.FindAll(_ => _.IsCaptured)[CountrySelectorDropDown.value])).ToString())
                    >= float.Parse(CountrySelectorInput.text))
                    {
                        result = true;
                        finished = true;
                    }
                }
                else
                {
                    result = true;
                    finished = true;
                }
            }
            else if (!isAmount)
            {
                result = true;
                finished = true;
            }

        });

        CountrySelectorCancelButton.onClick.AddListener(() =>
        {
            result = false;
            finished = true;
        });

        // صبر تا یکی از دکمه‌ها کلیک شود
        yield return new WaitUntil(() => finished);

        // برای جلوگیری از ثبت چندباره لیسنرها
        CountrySelectorOkButton.onClick.RemoveAllListeners();
        CountrySelectorCancelButton.onClick.RemoveAllListeners();

        // برگرداندن نتیجه
        CountrySelectorAnimator.SetTrigger("Close");
        uiBackground.SetActive(false);
        if (result)
        {
            if (CountrySelectorInput.text != "")
            {
                onResult?.Invoke(Countries.FindAll(_ => _.IsCaptured)[CountrySelectorDropDown.value], int.Parse(CountrySelectorInput.text));
            }
            else
            {
                onResult?.Invoke(Countries.FindAll(_ => _.IsCaptured)[CountrySelectorDropDown.value], 0);
            }


        }
        else onResult?.Invoke(null, 0);


    }

    [SerializeField] GameObject uiNewsTextPrefab;
    [SerializeField] GameObject uiNewsTextContent;
    [SerializeField] TMP_Text LatestNewsText;
    public List<string> Warlog;

    void WarlogLatestUpdate(string message)
    {
        Warlog.Add(message);
        LatestNewsText.text = "Latest News: " + message;
        if (Warlog.Count > 1)
        {
            var _instance = Instantiate(uiNewsTextPrefab, uiNewsTextContent.transform);
            _instance.GetComponent<TMP_Text>().text = Warlog[Warlog.Count - 2];
        }
    }

    public void CountryPanelClose() { countryPanelAnimator.SetTrigger("Close"); countryPanelAnimator.ResetTrigger("Open"); }
    public void NewsClose() { newsAnimator.SetTrigger("Close"); newsAnimator.ResetTrigger("Open"); }

    public void AttackStartButton() // Will call when clicked on Attack button 
    {
        CountryInformation attacker = null;
        StartCoroutine(CountrySelectorSet(null, false, (country, _) =>
        {
            attacker = country;
            if (country == null) return;
            var countries = Countries.Find(_ => _.Name == uiName.text);
            AttackStart(countries, attacker);
            CountryPanelRefresh();
        }));
    }

    public void DraftButton()
    {
        StartCoroutine(CountrySelectorSet("Population", true, (country, drafts) =>
        {
            if (country == null) return;
            country.Population -= drafts;
            country.Forces += drafts;
            int theindex = Countries.FindIndex(_ => _ == country);
            country.Income = Mathf.Round((float)Countries[theindex].Population / (float)InitCountriesPopulation[theindex] * InitCountriesIncome[theindex] * 100) / 100;
            CountryPanelRefresh();
        }));
    }

    public void DischargeButton()
    {
        StartCoroutine(CountrySelectorSet("Forces", true, (country, drafts) =>
        {
            if (country == null) return;
            country.Population += drafts;
            country.Forces -= drafts;
            int theindex = Countries.FindIndex(_ => _ == country);
            country.Income = Mathf.Round((float)Countries[theindex].Population / (float)InitCountriesPopulation[theindex] * InitCountriesIncome[theindex] * 100) / 100;
            CountryPanelRefresh();
        }));
    }
    public void BuyButton()
    {
        StartCoroutine(CountrySelectorSet("", true, (country, userInput) =>
        {
            if (country == null) return;
            if (userInput * 100000 <= Money)
            {
                country.Forces += userInput;
                money -= userInput * 1000000;
                CountryPanelRefresh();
            }
        }));
    }

    //Attack section

    public void AttackStart(CountryInformation defender, CountryInformation attacker)
    {


        float ratio = (attacker.Forces / Mathf.Max(0.1f, defender.Forces)) + Random.Range(-0.1f, 0.1f);
        if (ratio >= 1.2)
        {
            WarlogLatestUpdate(attacker.Name + " wins against " + defender.Name);
            attacker.Forces -= Mathf.CeilToInt(defender.Forces * 0.2f);
            defender.Forces -= Mathf.CeilToInt(attacker.Forces * 0.1f);

        }
        else if (ratio >= 0.8)
        {
            attacker.Forces -= Mathf.CeilToInt(defender.Forces * 0.4f);
            defender.Forces -= Mathf.CeilToInt(attacker.Forces * 0.4f);
            WarlogLatestUpdate(attacker.Name + " hurts " + defender.Name);
        }
        else
        {
            attacker.Forces -= Mathf.CeilToInt(defender.Forces * 0.1f);
            defender.Forces -= Mathf.CeilToInt(attacker.Forces * 0.2f);
            WarlogLatestUpdate(attacker.Name + " loses against " + defender.Name);
        }

        if (defender.Forces <= 0)
        {
            if (attacker.IsCaptured)
            {
                CountryState(defender, true);
                WarlogLatestUpdate(defender.Name + " captured");
            }
            else if (defender.IsCaptured)
            {
                CountryState(defender, false);
                WarlogLatestUpdate(defender.Name + " lost");
            }
        }
        else if (attacker.Forces <= 0)
        {
            if (attacker.IsCaptured)
            {
                CountryState(attacker, false);
                WarlogLatestUpdate(attacker.Name + " lost");
            }
            else if (defender.IsCaptured)
            {
                CountryState(attacker, true);
                WarlogLatestUpdate(attacker.Name + " captured");
            }
        }
        if (Countries.FindAll(_ => _.IsCaptured).Count == 0)
        {
            print("You lost");
            Application.Quit();
        }

        if (attacker.Forces < 0) attacker.Forces = 0;
        if (defender.Forces < 0) defender.Forces = 0;
    }

}
