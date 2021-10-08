﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using UnityEngine;

namespace HKVocals
{
    public class DreamDialogueAction : FsmStateAction
        {
            public string convName { get => convNames[0]; set => convNames = new string[] { value }; }
            public string sheetName { get => sheetNames[0]; set => sheetNames = new string[] { value }; }
            public string[] convNames;
            public string[] sheetNames;
            public float waitTime = 0;
            public ConvoMode convoMode = ConvoMode.Once;
            public int[] convoOccurances = new int[] { 0 };
            public bool isEnemy = true;
            private int lastIndex = int.MaxValue;
            private int realLastIndex = -1;
            public DreamDialogueAction(string convName, string sheetName)
            {
                this.convName = convName;
                this.sheetName = sheetName;
            }
            public DreamDialogueAction(string[] convNames, string sheetName)
            {
                this.convNames = convNames;
                this.sheetName = sheetName;
            }
            public DreamDialogueAction(List<(string, string)> Conversations)
            {
                convNames = Conversations.Select(tup => tup.Item1).ToArray();
                sheetNames = Conversations.Select(tup => tup.Item2).ToArray();
            }
            public override void OnEnter()
            {
                //if (_globalSettings.testSetting == 0)
                StartShow();
                if (Fsm != null)
                    Finish();
            }
            private void StartShow()
            {
                realLastIndex++;
                int currentOccurance;
                if (convoOccurances.Length <= realLastIndex)
                {
                    currentOccurance = convoOccurances.Last();
                }
                else
                {
                    currentOccurance = convoOccurances[realLastIndex];
                }
                
                if (currentOccurance == -1)
                {
                    return;
                }
                if (waitTime == 0)
                {
                    ShowDialogue();
                }
                else
                {
                    HKVocals.instance.coroutineSlave.StartCoroutine(WaitShowDialogue());
                }
            }
            private IEnumerator WaitShowDialogue()
            {
                yield return new WaitForSeconds(waitTime);
                ShowDialogue();
            }
            private void ShowDialogue()
            {
                switch (convoMode)
                {
                    case ConvoMode.Once:
                        if (lastIndex > convNames.Length - 2)
                        {
                            return;
                        }

                        if (lastIndex == int.MaxValue || lastIndex < -1)
                        {
                            lastIndex = -1;
                        }
                        lastIndex++;
                        break;
                    case ConvoMode.Repeat:
                        if (lastIndex == int.MaxValue || lastIndex > convNames.Length - 2 || lastIndex < -1)
                        {
                            lastIndex = -1;
                        }
                        lastIndex++;
                        break;
                    case ConvoMode.Random:
                        lastIndex = UnityEngine.Random.Range(0, convNames.Length);
                        break;
                }
                HKVocals.instance.CreateDreamDialogue(convNames.Length > lastIndex ? convNames[lastIndex] : convNames.Last(), sheetNames.Length == 0 ? "Enemy Dreams" : sheetNames.Length > lastIndex ? sheetNames[lastIndex] : sheetNames.Last(), "", "", isEnemy ? Owner : null);
            }
            public enum ConvoMode
            {
                Once,
                Repeat,
                Random
            }
        }
}