using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Phase { Waiting, NewRule, HordeArrives, Decide, Resolve, GameOver }

public class GamePlay : GamePlayBase
{
    public GameObject visitorsPrefab;

    [Tooltip("Willekeurige wachttijd tussen rondes, minimum en maximum in seconden.")]
    public float minWaitTime = 2f;
    public float maxWaitTime = 5f;
    [Tooltip("Minimale wachttijd na een verloren leven.")]
    public float waitAfterDeath = 5f;
    public float decideTime = 7f;
    public int roundsPerNewRule = 1;
    [Tooltip("Seconden na het kijken voordat je opnieuw door het raam kunt kijken.")]
    public float windowCooldown = 1.5f;
    [Tooltip("Aantal goede keuzes achter elkaar voor een extra leven.")]
    public int streakForExtraLife = 4;
    public int maxLives = 3;
    [Tooltip("Willekeurige tijd tussen het kloppen terwijl de horde voor de deur staat.")]
    public float knockMinInterval = 1.5f;
    public float knockMaxInterval = 4f;
    [Tooltip("Seconden voetstappen voordat de horde verschijnt.")]
    public float footstepsLead = 1.5f;
    [Tooltip("Resterende beslistijd waarop TimeRunningOut afgaat.")]
    public float timeRunningOutAt = 2f;

    Phase phase = Phase.Waiting;
    bool doorRequested = false;
    bool openedDoor = false;
    bool isPeeking = false;
    float windowReadyTime = 0f;

    void Awake()
    {
        RuleSet.all = new List<Rule> {
            new Rule("Box1 is left", () => GamePlayHelpers.GetX("Box1") < 0.0f),
            new Rule("Box2 is left", () => GamePlayHelpers.GetX("Box2") < 0.0f),
            new Rule("Box3 is left", () => GamePlayHelpers.GetX("Box3") < 0.0f),
            new Rule("Box1 is visible", () => GamePlayHelpers.IsVisible("Box1")),
            new Rule("Box2 is visible", () => GamePlayHelpers.IsVisible("Box2")),
            new Rule("Box3 is visible", () => GamePlayHelpers.IsVisible("Box3")),
        };

    }

    // Gekoppeld aan de ClickEventTriggers van deur en raam. De huidige fase
    // bepaalt of een klik iets doet.
    public void OnDoorClicked()
    {
        if (phase == Phase.Decide && !isPeeking) doorRequested = true;
        else if (phase == Phase.Waiting) AudioManager.PlaySound("DoorLocked");
    }

    public void OnWindowClicked()
    {
        if (phase != Phase.Waiting && phase != Phase.Decide) return;
        if (isPeeking || Time.time < windowReadyTime) return;
        StartCoroutine(Peek());
    }

    // Het kijken loopt naast de game loop. De fases wachten met de overgang
    // tot isPeeking weer false is, zodat er niets verschijnt tijdens het kijken.
    IEnumerator Peek()
    {
        isPeeking = true;
        AudioManager.PlaySound("WindowPeek");
        yield return cameraZoom.PeekThroughWindow();
        windowReadyTime = Time.time + windowCooldown;
        isPeeking = false;
    }

    override protected IEnumerator MainGameLoop()
    {
        RuleSet ruleset = new RuleSet();

        int round = 0;
        bool lostLife = false;
        int goodStreak = 0;
        while (gameIsRunning && lives > 0) {
            round++;

            phase = Phase.Waiting;
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            if (lostLife) waitTime = Mathf.Max(waitTime, waitAfterDeath);
            yield return new WaitForSeconds(waitTime);
            yield return new WaitWhile(() => isPeeking);

            phase = Phase.NewRule;
            if (ruleset.Count == 0 || round % roundsPerNewRule == 0) {
                string rule = ruleset.AddRule();
                if (rule != "") {
                    AudioManager.PlaySound("NewRule");
                    yield return ShowMessage($"New Rule: {rule}");
                }
            }

            phase = Phase.HordeArrives;
            AudioManager.PlaySound("Footsteps");
            yield return new WaitForSeconds(footstepsLead);
            GameObject visitors = Instantiate(visitorsPrefab);
            AudioManager.PlaySound("VisitorsArrive");

            phase = Phase.Decide;
            yield return Decide();

            phase = Phase.Resolve;
            int livesBefore = lives;
            yield return Resolve(ruleset);
            lostLife = lives < livesBefore;

            Destroy(visitors);

            goodStreak = lostLife ? 0 : goodStreak + 1;
            if (goodStreak >= streakForExtraLife) {
                goodStreak = 0;
                if (lives < maxLives) {
                    lives++;
                    AudioManager.PlaySound("ExtraLife");
                    yield return ShowMessage("Extra life!");
                }
            }
        }

        // De ronde waarin je je laatste leven verloor telt niet als overleefd.
        int roundsSurvived = round - 1;

        phase = Phase.GameOver;
        AudioManager.PlaySound("GameOver");
        yield return ShowMessage($"Game Over\nYou survived {roundsSurvived} {(roundsSurvived == 1 ? "round" : "rounds")}", true);
    }

    // Wacht tot de speler de deur opent of de tijd op is. Door het raam kijken
    // pauzeert de beslistijd. Ondertussen klopt de horde af en toe aan.
    IEnumerator Decide()
    {
        doorRequested = false;
        openedDoor = false;

        float elapsed = 0f;
        float knockTimer = 0f;
        float nextKnock = Random.Range(knockMinInterval, knockMaxInterval);
        bool warnedTimeRunningOut = false;
        while (elapsed < decideTime) {
            if (doorRequested) {
                openedDoor = true;
                yield break;
            }

            yield return null;
            if (!isPeeking) elapsed += Time.deltaTime;

            if (!warnedTimeRunningOut && decideTime - elapsed <= timeRunningOutAt) {
                AudioManager.PlaySound("TimeRunningOut");
                warnedTimeRunningOut = true;
            }

            knockTimer += Time.deltaTime;
            if (knockTimer >= nextKnock) {
                AudioManager.PlaySound("Knocking");
                knockTimer = 0f;
                nextKnock = Random.Range(knockMinInterval, knockMaxInterval);
            }
        }

        yield return new WaitWhile(() => isPeeking);
    }

    // Deur open + onveilig: dood. Deur dicht + veilig: je hebt ten onrechte
    // niet opengedaan. In beide gevallen gaat er een leven af.
    IEnumerator Resolve(RuleSet ruleset)
    {
        if (openedDoor) {
            yield return cameraZoom.GoToDoorAndOpen();

            bool killed = !ruleset.TestAll();
            if (killed) {
                AudioManager.PlaySound("JumpScare");
                yield return cameraZoom.KillerSequence();
                lives--;
            }
            else {
                AudioManager.PlaySound("TrickOrTreat");
            }

            yield return new WaitForSeconds(1f);

            if (killed) AudioManager.PlaySound("Death");
            else AudioManager.PlaySound("DoorClose");

            cameraZoom.CloseDoor();
            cameraZoom.ResetToStart();
        }
        else {
            if (cameraZoom.TestFromDoorView(ruleset)) {
                lives--;
                AudioManager.PlaySound("VisitorsDisappointed");
                yield return ShowMessage("It was safe... you should have opened the door!");
            }
            else {
                AudioManager.PlaySound("ImpostorsLeave");
            }
        }
    }

}
