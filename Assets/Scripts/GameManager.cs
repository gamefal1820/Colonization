using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.IntegerTime;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public TextAsset NewgamejsonFile;

    public int Money;

    public bool Startgame;

    public List<CountryInformation> Countries;

    void Awake()
    {
        Instance = this;


        //load the game as a new game
        var data = JsonUtility.FromJson<CountryListWrapper>(NewgamejsonFile.text);
        Countries = data.Countries;
        Money = data.Money;
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
            print("#" + weights[i] + " "+possibleTargets[i].Name);
        }
        
        
        int randomValue = Random.Range(0, totalWeight+1);
        int cumulative = 0;

        for (int i = 0; i < possibleTargets.Count; i++)
        {
            cumulative += weights[i];
            if (randomValue <= cumulative)
            {
                var target = possibleTargets[i];
                AttackStart(ref target, ref attacker);
                print("cumulative: "+ cumulative);
                return;
            }
            
        }
    }

    //UI section

    [SerializeField] TMP_Text uiName;
    [SerializeField] TMP_Text uiPopulation;
    [SerializeField] TMP_Text uiForces;
    [SerializeField] TMP_Text uiIncome;
    [SerializeField] Slider uiDiplomacyRate;
    [SerializeField] Animator countryPanelAnimator;
    [SerializeField] Animator newsAnimator;


    public void CountryPanelSet(CountryInformation info)
    {
        uiName.text = info.Name;
        uiPopulation.text = "Population: " + info.Population + "M";
        uiForces.text = "Forces: " + info.Population + "K";
        uiIncome.text = "Income: +" + info.Income + "K";
        uiDiplomacyRate.value = info.DeplomacyRate;
        //CountryPanel Animation
        countryPanelAnimator.SetTrigger("Open");
    }
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

    [SerializeField] GameObject uiNewsTextPrefab;
    [SerializeField] GameObject uiNewsTextContent;
    [SerializeField] TMP_Text LatestNewsText;
    public List<string> Warlog;
    void WarlogLatestUpdate()
    {
        LatestNewsText.text = "Latest News: " + Warlog.Last();
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
        var countries = Countries.Find(_ => _.Name == uiName.text);
        CountryInformation attacker = null;
        AttackStart(ref countries, ref attacker);
    }

    //Attack section

    public void AttackStart(ref CountryInformation defender, ref CountryInformation attacker)
    {
        if (attacker == null)
        {
            // Just for test
            // Later, Should add a pop-up menu for choosing country
            attacker = Countries.Find(_ => _.Name == "Iran");
        }

        float ratio = (attacker.Forces / Mathf.Max(0.1f, defender.Forces)) + Random.Range(-0.1f, 0.1f);
        if (ratio >= 1.2)
        {
            Warlog.Add(attacker.Name + " wins against " + defender.Name);
            WarlogLatestUpdate();
            attacker.Forces -= Mathf.Ceil(attacker.Forces * 0.1f);
            defender.Forces -= Mathf.Ceil(defender.Forces * 0.2f);
            if (defender.Forces == 0 && attacker.IsCaptured)
            {
                defender.IsCaptured = true;
                Warlog.Add(defender.Name + " captured");
                WarlogLatestUpdate();
            }
        }
        else if (ratio >= 0.8)
        {
            attacker.Forces -= Mathf.Ceil(attacker.Forces * 0.4f);
            defender.Forces -= Mathf.Ceil(defender.Forces * 0.4f);
            Warlog.Add(attacker.Name + " hurts " + defender.Name);
            WarlogLatestUpdate();
        }
        else
        {
            attacker.Forces -= Mathf.Ceil(attacker.Forces * 0.2f);
            defender.Forces -= Mathf.Ceil(defender.Forces * 0.1f);
            if (attacker.Forces <= 0 && attacker.IsCaptured)
            {
                attacker.IsCaptured = false;
                Warlog.Add(attacker.Name + " lost");
                if (Countries.FindAll(_ => _.IsCaptured).Count == 0)
                {
                    print("You lost");
                    Application.Quit();
                }
            }
            Warlog.Add(attacker.Name + " loses against " + defender.Name);
            WarlogLatestUpdate();
        }
        if (attacker.Forces < 0) attacker.Forces = 0;
        if (defender.Forces < 0) defender.Forces = 0;
    }

}
