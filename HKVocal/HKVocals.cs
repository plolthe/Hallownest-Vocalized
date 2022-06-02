namespace HKVocals;

public class HKVocals: Mod, IGlobalSettings<GlobalSettings>, ILocalSettings<SaveSettings>, ICustomMenuMod
{
    public static GlobalSettings _globalSettings { get; set; } = new GlobalSettings();
    public void OnLoadGlobal(GlobalSettings s) => _globalSettings = s;
    public GlobalSettings OnSaveGlobal() => _globalSettings;
    public SaveSettings _saveSettings { get; set; } = new SaveSettings();
    public void OnLoadLocal(SaveSettings s) => _saveSettings = s;
    public SaveSettings OnSaveLocal() => _saveSettings;
        
    public const bool RemoveOrigNPCSounds = true;
    public AssetBundle audioBundle;
    public AudioSource audioSource;
    public Coroutine autoTextRoutine;
    internal static HKVocals instance;
    public bool ToggleButtonInsideMenu => false;
    public bool IsGrubRoom = false;
    public string GrubRoom = "Crossroads_48";
    public static NonBouncer CoroutineHolder;
    public static bool PlayDNInFSM = true;

    public HKVocals() : base("Hollow Knight Vocalized")
    {
        var go = new GameObject("HK Vocals Coroutine Holder");
        CoroutineHolder = go.AddComponent<NonBouncer>();
        Object.DontDestroyOnLoad(CoroutineHolder);
    }
    public override string GetVersion() => "0.0.0.1";

    public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates) => ModMenu.CreateModMenuScreen(modListMenu);
        
    public override void Initialize()
    {
        instance = this;
        On.DialogueBox.ShowPage += ShowPage;
        On.DialogueBox.ShowNextPage += StopConvo_NextPage;
        On.DialogueBox.HideText += StopConvo_HideText;
        On.PlayMakerFSM.Awake += FSMAwake;
        On.HutongGames.PlayMaker.Actions.AudioPlayerOneShot.DoPlayRandomClip += PlayRandomClip;
        On.EnemyDreamnailReaction.Start += EDNRStart;
        On.EnemyDreamnailReaction.ShowConvo += ShowConvo;
        On.HealthManager.TakeDamage += TakeDamage;
        UIManager.EditMenus +=  ModMenu.AddAudioSlider;
        
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged += EternalOrdeal.DeleteZoteAudioPlayersOnSceneChange;
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged += ZoteLever.SetZoteLever;
        On.BossStatueLever.OnTriggerEnter2D += ZoteLever.UseZoteLever;

        LoadAssetBundle();
        CreateAudioSource();
    }

    private void StopConvo_HideText(On.DialogueBox.orig_HideText orig, DialogueBox self)
    {
        audioSource.Stop();
        orig(self);
    }

    private void StopConvo_NextPage(On.DialogueBox.orig_ShowNextPage orig, DialogueBox self)
    {
        string key = self.currentConversation + "%%" + self.currentPage;
        if (_globalSettings.scrollLock && !_saveSettings.FinishedConvos.Contains(key))
        {
            return;
        }
        //Log(key);
        _saveSettings.FinishedConvos.Add(key);
        audioSource.Stop();
        orig(self);
    }

    public void CreateAudioSource()
    {
        Log("creating asrc");
        GameObject audioGO = new GameObject("HK Vocals Audio");
        audioSource = audioGO.AddComponent<AudioSource>();
        Object.DontDestroyOnLoad(audioGO);
    }

    private void TakeDamage(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
    {
        orig(self, hitInstance);
        for (int i = 0; i < Dictionaries.HpListeners.Count; i++)
        {
            if (Dictionaries.HpListeners[i](self))
            {
                Dictionaries.HpListeners.RemoveAt(i);
                i--;
            }
        }
    }

    private void ShowConvo(On.EnemyDreamnailReaction.orig_ShowConvo orig, EnemyDreamnailReaction self)
    {
        if (!_globalSettings.dnDialogue)
        {
            orig(self);
        }
        else
        {
            //get the fsm
            PlayMakerFSM fsm = FsmVariables.GlobalVariables.GetFsmGameObject("Enemy Dream Msg").Value.LocateMyFSM("Display");
            
            bool found = false;
            
            string enemyName = self.gameObject.name;
            
            //get vanilla dn variables
            int convoAmount = ReflectionHelper.GetField<EnemyDreamnailReaction, int>(self, "convoAmount");
            string convoTitle = ReflectionHelper.GetField<EnemyDreamnailReaction, string>(self, "convoTitle");

            //see if the convo title exists as a field in DN audios class
            var listField = typeof(DNAudios).GetField(convoTitle);
            
            if (listField == null)
            {
                LogDebug($"{enemyName} with {convoTitle} isnt in DN List");
                found = false;
            }
            else
            {
                //get an actual list
                List<string> dnAudioList = (List<string>)listField.GetValue(null);
                
                //find the name that will be in audiobundle
                string standardName = dnAudioList.FirstOrDefault(s => enemyName.StartsWith(s));
                if (standardName == null)
                {
                    LogDebug($"{enemyName} isnt in {dnAudioList} List");
                    found = false;
                }
                else
                {
                    //randomly select convo amount
                    int selectedConvoAmount = UnityEngine.Random.Range(1, convoAmount);
                    //find all audios that match this format
                    string bundleAudioName = $"${standardName}$_{convoTitle}_{convoAmount}";
                    
                    //acount for GB1,GB2 and GH. (enemy name isnt used here)
                    if (listField.Name is "GH" or "GB1" or "GB2")
                    {
                        bundleAudioName = $"${listField.Name}$_{convoTitle}_{convoAmount}";
                    }
                    
                    
                    List<string> availableAudios = Dictionaries.audioNames.FindAll(s => s.StartsWith(bundleAudioName));
                    if (availableAudios == null || availableAudios.Count == 0)
                    {
                        LogDebug($"{bundleAudioName} isnt in audioBundle");
                    }
                    //choose random and play
                    LogDebug("DN Audio Found :)");
                    int randomVA = Random.Range(1, availableAudios.Count);
                    AudioUtils.TryPlayAudioFor($"{bundleAudioName}_{randomVA}");
                    found = true;
                }
            }

            //find audio
            if (found)
            {
                PlayDNInFSM = false;
                fsm.Fsm.GetFsmString("Convo Title").Value = convoTitle;
                fsm.Fsm.GetFsmInt("Convo Amount").Value = convoAmount;
                
                //normally the globla transision sends it to Check Convo and there we set PlayDNInFSM. I wanna bypass that
                fsm.SetState("Cancel Existing");
                
                //play audio
            }
            else
            {
                orig(self);
            }
        }
    }

    private void EDNRStart(On.EnemyDreamnailReaction.orig_Start orig, EnemyDreamnailReaction self)
    {
        orig(self);
        //if (self.GetComponent<EnemyDeathEffects>() != null)
        self.gameObject.AddComponent<ExDNailReaction>();
    }

    private void PlayRandomClip(On.HutongGames.PlayMaker.Actions.AudioPlayerOneShot.orig_DoPlayRandomClip orig, AudioPlayerOneShot self)
    {
        orig(self);
        if (!RemoveOrigNPCSounds /*&& _globalSettings.testSetting == 0*/ && self.Fsm.Name == "Conversation Control")
        {
            HKVocals.CoroutineHolder.StartCoroutine(FadeOutClip(ReflectionHelper.GetField<AudioPlayerOneShot, AudioSource>(self, "audio")));
        }
    }

    private void FSMAwake(On.PlayMakerFSM.orig_Awake orig, PlayMakerFSM self)
    {
        orig(self);
        /*if (self.FsmGlobalTransitions.Any(t => t.EventName.ToLower().Contains("dream")))
        {
            self.MakeLog();
            foreach (FsmTransition t in self.FsmGlobalTransitions)
                Log(t.EventName);
        }*/
        if (Dictionaries.SceneFSMEdits.TryGetValue((UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, self.gameObject.name, self.FsmName), out var sceneAction))
            sceneAction(self);
        if (Dictionaries.GoFSMEdits.TryGetValue((self.gameObject.name, self.FsmName), out var goAction))
            goAction(self);
        if (Dictionaries.FSMChanges.TryGetValue(self.FsmName, out var action))
            action(self);
        /*if (self.gameObject.name.ToLower().Contains("elderbug"))
        {
            foreach (FsmVar v in self.FsmStates.SelectMany(s => s.Actions.Where(a => a is CallMethodProper call && call.behaviour.Value.ToLower() == "dialoguebox").Cast<CallMethodProper>().SelectMany(c => c.parameters)))
                Log(v.variableName + "  " + v.Type + "  " + v.GetValue());
        }*/
    }

    private void ShowPage(On.DialogueBox.orig_ShowPage orig, DialogueBox self, int pageNum)
    {
        orig(self, pageNum);
        var convo = self.currentConversation + "_" + (self.currentPage - 1);
        LogDebug($"Showing page in {convo}");
        if (self.currentPage - 1 == 0)
        {
            AudioUtils.TryPlayAudioFor(convo,37f/60f);
        }
        else
        {
            AudioUtils.TryPlayAudioFor(convo, 3f/4f);
        }
        if (audioSource.isPlaying)
        {
            if (autoTextRoutine != null)
            {
                HKVocals.CoroutineHolder.StopCoroutine(autoTextRoutine);
            }
            autoTextRoutine = HKVocals.CoroutineHolder.StartCoroutine(AutoChangePage(self));
        }
    }

    private void SetConversation(On.DialogueBox.orig_SetConversation orig, DialogueBox self, string convName,
        string sheetName)
    {
        orig(self, convName, sheetName);
        Log("Started Conversation " + convName + " " + sheetName);
        //if (_globalSettings.testSetting == 0)
        AudioUtils.TryPlayAudioFor(convName);
    }

    public void CreateDreamDialogue(string convName, string sheetName, string enemyType = "", string enemyVariant = "", GameObject enemy = null)
    {
        PlayMakerFSM fsm = FsmVariables.GlobalVariables.GetFsmGameObject("Enemy Dream Msg").Value.LocateMyFSM("Display");
        fsm.Fsm.GetFsmString("Convo Title").Value = convName;
        fsm.Fsm.GetFsmString("Sheet").Value = sheetName;
        fsm.SendEvent("DISPLAY DREAM MSG");
    }

    public IEnumerator AutoChangePage(DialogueBox dialogueBox)
    {
        int newPageNum = dialogueBox.currentPage + 1;
        string oldConvoName = dialogueBox.currentConversation;
        yield return new WaitWhile(() => AudioUtils.IsPlaying() && dialogueBox && dialogueBox.currentPage < newPageNum && dialogueBox.currentConversation == oldConvoName);
        yield return new WaitForSeconds(1f/6f);//wait additional 1/6th second
        if (_globalSettings.autoScroll &&
            dialogueBox != null &&
            dialogueBox.currentPage < newPageNum &&
            dialogueBox.currentConversation == oldConvoName)
        {
            dialogueBox.ShowNextPage();
        }
    }

    private IEnumerator FadeOutClip(AudioSource source)
    {
        float volumeChange = source.volume / 100f;
        yield return new WaitForSeconds(1f);
        for (int i = 0; i < 100; i++)
            source.volume -= volumeChange;
    }

    private void LoadAssetBundle()
    {
        Assembly asm = Assembly.GetExecutingAssembly();
        audioBundle = AssetBundle.LoadFromStream(asm.GetManifestResourceStream("HKVocals.audiobundle"));
        string[] allAssetNames = audioBundle.GetAllAssetNames();
        for (int i = 0; i < allAssetNames.Length; i++)
        {
            if (Dictionaries.audioExtentions.Any(ext => allAssetNames[i].EndsWith(ext)))
            {
                Dictionaries.audioNames.Add(Path.GetFileNameWithoutExtension(allAssetNames[i]).ToUpper());
            }
            LogDebug($"Object in audiobundle: {allAssetNames[i]} {Path.GetFileNameWithoutExtension(allAssetNames[i])?.ToUpper().Replace("KNGHT", "KNIGHT")}");
        }
    }
}