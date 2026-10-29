using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Phase { Waiting, NewRule, HordeArrives, Decide, Resolve, GameOver }

public class GamePlay : GamePlayBase
{
    public GameObject visitorsPrefab;
    public GameObject gardenPrefab;

    [Tooltip("Random wait time between rounds, minimum and maximum in seconds.")]
    public float minWaitTime = 2f;
    public float maxWaitTime = 5f;
    [Tooltip("Minimum wait time after losing a life.")]
    public float waitAfterDeath = 5f;
    public float decideTime = 7f;
    public int roundsPerNewRule = 3;
    [Tooltip("Chance that a rule change removes a rule instead of adding one.")]
    [Range(0f, 1f)]
    public float removeRuleChance = 0.5f;
    [Tooltip("Chance that a rule change turns an old rule on again instead of adding a new one.")]
    [Range(0f, 1f)]
    public float reactivateRuleChance = 0.5f;
    [Tooltip("Seconds a rule change message stays on screen.")]
    public float ruleMessageDuration = 5f;
    [Tooltip("Seconds after peeking before you can look through the window again.")]
    public float windowCooldown = 1.5f;
    [Tooltip("Seconds the lights stay off after clicking the light switch.")]
    public float lightsOffDuration = 1.5f;
    [Tooltip("Seconds after the lights are back on before the switch works again.")]
    public float lightSwitchCooldown = 5f;
    [Tooltip("Number of correct choices in a row for an extra life.")]
    public int streakForExtraLife = 4;
    public int maxLives = 3;
    [Tooltip("Random time between knocks while the horde is at the door.")]
    public float knockMinInterval = 1.5f;
    public float knockMaxInterval = 4f;
    [Tooltip("Seconds of footsteps before the horde appears.")]
    public float footstepsLead = 1.5f;
    [Tooltip("Remaining decision time at which TimeRunningOut plays.")]
    public float timeRunningOutAt = 2f;

    Phase phase = Phase.Waiting;
    bool doorRequested = false;
    // Turning off the lights sends the horde away: the decision ends as if
    // the door stayed closed.
    bool hordeSentAway = false;
    bool openedDoor = false;
    bool isPeeking = false;
    float windowReadyTime = 0f;
    bool lightsOff = false;
    float lightSwitchReadyTime = 0f;

    void Awake()
    {
        RuleSet.all = new List<Rule> {
            new Rule("no one is wearing a purple hat", "someone was wearing a purple hat", () => !GamePlayHelpers.Exists("Hat1")),
            new Rule("someone is wearing a black hat", "no one was wearing a black hat", () => GamePlayHelpers.Exists("Hat2")),
            new Rule("there are an even amount of tricksters", "the amount of visitors was uneven", () => GamePlayHelpers.CountIsEven("Trickster")),
            new Rule("there are an uneven amount of pumpkins", "the amount of pumpkins was even", () => !GamePlayHelpers.CountIsEven("Pumpkin1")),
        };

    }

    public void OnLightSwitchClicked()
    {
        if (lightsOff || Time.time < lightSwitchReadyTime) return;
        AudioManager.PlaySound("TurnLightOff");
        if (phase == Phase.Decide && !isPeeking) hordeSentAway = true;
        StartCoroutine(LightsOut());
    }

    // Turns off the ambient light and all lights in the game for a moment.
    // Only the lights that were on are turned on again afterwards.
    IEnumerator LightsOut()
    {
        lightsOff = true;
        UnityEngine.Rendering.AmbientMode oldAmbientMode = RenderSettings.ambientMode;
        Color oldAmbientLight = RenderSettings.ambientLight;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.black;

        List<Light> turnedOff = new List<Light>();
        foreach (Light light in GamePlayHelpers.root.GetComponentsInChildren<Light>())
        {
            if (!light.enabled) continue;
            light.enabled = false;
            turnedOff.Add(light);
        }

        yield return new WaitForSeconds(lightsOffDuration);

        RenderSettings.ambientMode = oldAmbientMode;
        RenderSettings.ambientLight = oldAmbientLight;
        foreach (Light light in turnedOff)
        {
            // The garden or visitors may have been destroyed in the meantime.
            if (light != null) light.enabled = true;
        }

        lightSwitchReadyTime = Time.time + lightSwitchCooldown;
        lightsOff = false;
    }

    // Hooked up to the ClickEventTriggers of the door and window. The current
    // phase determines whether a click does anything.
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

    // Peeking runs alongside the game loop. The phases hold off on transitioning
    // until isPeeking is false again, so nothing appears while peeking.
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

        GameObject garden = GamePlayHelpers.Find("Garden").gameObject;
        int round = 0;
        bool lostLife = false;
        int goodStreak = 0;
        while (gameIsRunning && lives > 0) {
            round++;
            yield return new WaitWhile(() => isPeeking);
            if (garden != null) Destroy(garden);
            garden = Instantiate(gardenPrefab, GamePlayHelpers.root, true);
            garden.name = gardenPrefab.name;

            phase = Phase.Waiting;
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            if (lostLife) waitTime = Mathf.Max(waitTime, waitAfterDeath);
            yield return new WaitForSeconds(waitTime);
            yield return new WaitWhile(() => isPeeking);

            phase = Phase.NewRule;
            if (ruleset.Count == 0 || round % roundsPerNewRule == 0) {
                // Removing is only possible if at least one rule remains.
                // Adding is either a new rule or turning an old one on again.
                bool canRemove = ruleset.Count > 1;
                bool canAddNew = ruleset.CanAddRule;
                bool canReactivate = ruleset.InactiveCount > 0;
                bool canAdd = canAddNew || canReactivate;
                bool remove = canRemove && (!canAdd || Random.value < removeRuleChance);
                bool reactivate = canReactivate && (!canAddNew || Random.value < reactivateRuleChance);

                if (remove) {
                    string rule = ruleset.DeactivateRule(out int number);
                    AudioManager.PlaySound("NewRule");
                    yield return ShowMessage($"Rule #{number} removed: You no longer have to check if\n{rule}", false, ruleMessageDuration);
                }
                else if (reactivate) {
                    string rule = ruleset.ActivateRule(out int number);
                    AudioManager.PlaySound("NewRule");
                    yield return ShowMessage($"Rule #{number} is again in action: |Check if\n{rule}", false, ruleMessageDuration);
                }
                else if (canAdd) {
                    string rule = ruleset.AddRule(out int number);
                    AudioManager.PlaySound("NewRule");
                    yield return ShowMessage($"Rule #{number}: |Only open the door if\n{rule}", false, ruleMessageDuration);
                }
            }

            phase = Phase.HordeArrives;
            AudioManager.PlaySound("Footsteps");
            yield return new WaitForSeconds(footstepsLead);
            // Parent under the Scene root, otherwise the rules can't find the visitors.
            GameObject visitors = Instantiate(visitorsPrefab, GamePlayHelpers.root, true);
            visitors.name = visitorsPrefab.name;
            AudioManager.PlaySound("VisitorsArrive");

            phase = Phase.Decide;
            yield return Decide();

            phase = Phase.Resolve;
            int livesBefore = lives;
            yield return Resolve(ruleset, visitors);
            lostLife = lives < livesBefore;

            // After the last life the visitors stay, so the killer remains in view.
            if (lives > 0) Destroy(visitors);

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

        // The round in which you lost your last life doesn't count as survived.
        int roundsSurvived = round - 1;

        phase = Phase.GameOver;
        AudioManager.PlaySound("GameOver");
        yield return ShowMessage($"Game Over\nYou survived {roundsSurvived} {(roundsSurvived == 1 ? "round" : "rounds")}", true);
    }

    // Waits until the player opens the door, turns off the lights or time runs out.
    // Peeking through the window pauses the decision time. Meanwhile the horde
    // knocks now and then.
    IEnumerator Decide()
    {
        doorRequested = false;
        hordeSentAway = false;
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
            if (hordeSentAway) yield break;

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

    // Door open + unsafe: death. Door closed + safe: you wrongly didn't
    // open up. Either way you lose a life.
    IEnumerator Resolve(RuleSet ruleset, GameObject visitors)
    {
        List<string> brokenRules = ruleset.FailedRules();
        bool safe = brokenRules.Count == 0;

        if (openedDoor) {
            yield return cameraZoom.GoToDoorAndOpen();

            bool killed = !safe;
            if (killed) {
                AudioManager.PlaySound("JumpScare");
                yield return cameraZoom.KillerSequence();
            }
            else {
                AudioManager.PlaySound("TrickOrTreat");
            }

            yield return new WaitForSeconds(1f);

            if (killed) {
                AudioManager.PlaySound("Death");
                // The killer stays in view: first the reason, then the heart goes out.
                yield return ShowMessage($"You broke the {(brokenRules.Count == 1 ? "rule" : "rules")}:\n{string.Join("\n", brokenRules)}");
                yield return LoseLife();
            }
            else {
                AudioManager.PlaySound("DoorClose");
            }

            // When dead, the camera stays on the killer for the game over.
            if (lives > 0) {
                cameraZoom.CloseDoor();
                cameraZoom.ResetToStart();
            }
        }
        else {
            AudioManager.PlaySound(safe ? "VisitorsDisappointed" : "ImpostorsLeave");

            // Sent away by the lights: the horde vanishes in the dark while
            // grumbling, and the verdict follows once the lights are back on.
            // The rules were checked above, so hiding the visitors is fine.
            if (hordeSentAway) {
                visitors.SetActive(false);
                yield return new WaitWhile(() => lightsOff);
            }

            if (safe) {
                yield return ShowMessage("It was safe... you should have opened the door!");
                yield return LoseLife();
            }
            else {
                yield return ShowMessage("You didn't open the door. Smart choice.\nYou are safe...| for now");
            }
        }
    }

}
