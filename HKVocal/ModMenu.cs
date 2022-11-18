﻿using static UnityEngine.Object;

namespace HKVocals;
public static class ModMenu
{
    public static Menu MenuRef;
    public static MenuScreen CreateModMenuScreen(MenuScreen modListMenu)
    {
        MenuRef ??= new Menu("Hallownest Vocalised", new Element[]
        {
            new TextPanel("To change volume, please use Audio Menu"),

            Blueprints.HorizontalBoolOption("Scroll Lock", 
                "Should first time dialogues be scroll locked until audio has finished?",
                (i) => HKVocals._globalSettings.scrollLock = i,
                () => HKVocals._globalSettings.scrollLock),
            
            Blueprints.HorizontalBoolOption("Auto Scroll", 
                "Should dialogue autoscroll after the audio finishes",
                (i) => 
                    {
                        HKVocals._globalSettings.autoScroll = i;
                        MenuRef.Find("Auto Scroll Speed").isVisible = i;
                        MenuRef.Update();
                    }
                ,
                () => HKVocals._globalSettings.autoScroll),
            
            new HorizontalOption("Auto Scroll Speed", 
                "How fast should it autoscroll on audio finish",
                Enum.GetNames(typeof(MajorFeatures.AutoScroll.ScrollSpeed)).ToArray(),
                (i) => HKVocals._globalSettings.ScrollSpeed = (MajorFeatures.AutoScroll.ScrollSpeed) i,
                () => (int) HKVocals._globalSettings.ScrollSpeed,
                Id: "Auto Scroll Speed")
                {
                    isVisible = HKVocals._globalSettings.autoScroll
                },
            
            Blueprints.HorizontalBoolOption("Dream Nail Dialogues", 
                "Should dream nail dialogues be voiced?",
                (i) =>
                {
                    HKVocals._globalSettings.dnDialogue = i;
                    MenuRef.Find("Automatic Boss Dialogue").isVisible = !i;
                    MenuRef.Update();
                },
                () => HKVocals._globalSettings.dnDialogue),
            
            new HorizontalOption("Automatic Boss Shouts",
                "Should some bosses automatically do shouts",
                new []{"On", "Off"},
                i => HKVocals._globalSettings.automaticBossDialogue = i == 0,
                () => HKVocals._globalSettings.automaticBossDialogue ? 0 : 1,
                Id: "Automatic Boss Dialogue")
            {
                isVisible = !HKVocals._globalSettings.dnDialogue
            },
        });
            
        
        return MenuRef.GetMenuScreen(modListMenu);
    }

    public static void AddAudioSlider()
    {
        //get go of a current slider
        GameObject MusicSlider = UIManager.instance.gameObject.transform.Find("UICanvas/AudioMenuScreen/Content/MusicVolume/MusicSlider").gameObject;
            
        //make clone
        GameObject VolumeSlider = Instantiate(MusicSlider, MusicSlider.transform.parent);
            
        MenuAudioSlider VolumeSlider_MenuAudioSlider = VolumeSlider.GetComponent<MenuAudioSlider>();
        Slider VolumeSlider_Slider = VolumeSlider.GetComponent<Slider>();
            
        //all the other sliders are 0.6 down from each other
        VolumeSlider.transform.position -= new Vector3(0, 0.9f, 0f); 
        VolumeSlider.name = "HK Vocals Slider";

        Action<float> StoreValue = f =>
        {
            VolumeSlider_MenuAudioSlider.UpdateTextValue(f);
            HKVocals._globalSettings.Volume = (int)f;
        };

        // stuff to happen whenever slider is moved
        var SliderEvent = new Slider.SliderEvent();
        SliderEvent.AddListener(StoreValue.Invoke);

        VolumeSlider_Slider.onValueChanged = SliderEvent ;

        //change the key of the text so it can be changed
        Destroy(VolumeSlider.Find("Label").GetComponent<AutoLocalizeTextUI>());
        VolumeSlider.Find("Label").GetComponent<Text>().text = "HK Vocals Volume: ";
        VolumeSlider.SetActive(true);
            
        //to make sure when go is cloned, it gets the value of the previous session not the value of the music slider
        VolumeSlider_MenuAudioSlider.UpdateTextValue(HKVocals._globalSettings.Volume);
        VolumeSlider_Slider.value = HKVocals._globalSettings.Volume;
    }
}
