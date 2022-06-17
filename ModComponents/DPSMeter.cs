using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using BepInEx;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.UI;


namespace BetterUI
{

    static class DPSMeter
    {

        public class playerLog
        {
            public string className;
            public float stagePlayerDamage;
            public float stageMinionDamage;
            public float rollingPlayerDamage;
            public float rollingMinionDamage;
            public Queue<DamageLog> playerDamageLog;
            public Queue<DamageLog> minionDamageLog;

            public float playerDPS {get => playerDamageLog.Count > 0 ? rollingPlayerDamage / Clamp(Time.time - playerDamageLog.Peek().time) : 0; }
            public float minionDPS {get => minionDamageLog.Count > 0 ? rollingMinionDamage / Clamp(Time.time - minionDamageLog.Peek().time) : 0; }

            public playerLog(string cn, Queue<DamageLog> pdl, Queue<DamageLog> mdl)
            {
                className = cn;
                stagePlayerDamage = 0;
                stageMinionDamage = 0;
                rollingPlayerDamage = 0;
                rollingMinionDamage = 0;
                playerDamageLog = pdl;
                minionDamageLog = mdl;
            }

            public void updatePlayerDamages(float damage)
            {
                stagePlayerDamage += damage;
                rollingPlayerDamage += damage;
            }

            public void updateMinionDamages(float damage)
            {
                stageMinionDamage += damage;
                rollingMinionDamage += damage;
            }

            public void updateRollingDamage()
            {
                while(playerDamageLog.Count > 0 && playerDamageLog.Peek().time < Time.time - 10f)
                {
                    rollingPlayerDamage -= playerDamageLog.Dequeue().damage;
                }
                 while (minionDamageLog.Count > 0 && minionDamageLog.Peek().time < Time.time - 10f)
                {
                   rollingMinionDamage -= minionDamageLog.Dequeue().damage;
                }
            }
        }

        private static Dictionary<string, playerLog> userLogs;


        private static readonly Queue<DamageLog> characterDamageLog = new Queue<DamageLog>();
        private static float characterDamageSum = 0;
        private static readonly Queue<DamageLog> minionDamageLog = new Queue<DamageLog>();
        private static float minionDamageSum = 0;

        private static GameObject DPSMeterPanel;
        private static RoR2.UI.ChatBox chatBox;

        private static HGTextMeshProUGUI textMesh;
        public static float DPS { get => MinionDPS + CharacterDPS; }
        public static float CharacterDPS { get => characterDamageLog.Count > 0 ? characterDamageSum / Clamp(Time.time - characterDamageLog.Peek().time) : 0; }
        public static float MinionDPS { get => minionDamageLog.Count > 0 ? minionDamageSum / Clamp(Time.time - minionDamageLog.Peek().time) : 0; }

        private static int mode = 0;

        internal struct DamageLog
        {
            public float damage;
            public float time;
            public DamageLog(float dmg)
            {
                damage = dmg;
                time = Time.time;
            }
        }

        internal static void Initialize()
        {
              
            BetterUIPlugin.Hooks.Add<GlobalEventManager, DamageDealtMessage>("ClientDamageNotified", DamageDealtMessage_ClientDamageNotified);

            BetterUIPlugin.onEnable += () => BetterUIPlugin.onUpdate += onUpdate;
            BetterUIPlugin.onDisable += () => BetterUIPlugin.onUpdate -= onUpdate;
      
           

            if (ConfigManager.DPSMeterWindowShow.Value)
            {
                BetterUIPlugin.onEnable += () => BetterUIPlugin.onHUDAwake += onHUDAwake;
                BetterUIPlugin.onDisable += () => BetterUIPlugin.onHUDAwake -= onHUDAwake;

                if (ConfigManager.DPSMeterWindowHideWhenTyping.Value) BetterUIPlugin.Hooks.Add<RoR2.UI.ChatBox>(nameof(RoR2.UI.ChatBox.Awake), ChatBox_Awake);
            }
        }

        public static float Clamp(float value)
        {
            return Math.Min(Math.Max(1, value), ConfigManager.DPSMeterTimespan.Value);
        }

        private static void onUpdate()
        {

            if (Input.GetKeyUp(KeyCode.F2))
            {
                mode = (mode + 1) % 5;
            }
            if (userLogs != null)
            {
                           
                
                var tupleList = new List<(float, string)>{};
                var total = 1f;

                foreach(KeyValuePair <string, playerLog> kvp in userLogs)
                {
                    

                    var userName = kvp.Key;
                    var log = kvp.Value;

                    log.updateRollingDamage();

                    total += log.stagePlayerDamage + log.stageMinionDamage;
                    if (textMesh != null)
                    {

                        string s = userName;
                        float f = 0;
                        switch(mode)
                        {
                            case 0:
                                s += ": ";
                                f = log.playerDPS + log.minionDPS;
                                break;
                            case 1:
                                s += ": ";
                                f = log.stagePlayerDamage + log.stageMinionDamage;
                                break;
                            case 2:
                                s += ": ";
                                f = log.stagePlayerDamage + log.stageMinionDamage;
                                break;
                            case 3:
                                s += "'s minions: ";
                                f = log.minionDPS;
                                break;
                            case 4:
                                s += "'s minions: ";
                                f = log.stageMinionDamage;
                                break;
                            default:
                                s += ": ";
                                f = log.playerDPS;
                                break;
                        }

                        tupleList.Add((f, s));
                        
                        
                    }
                }

                tupleList.Sort((x,y) => y.Item1.CompareTo(x.Item1));
                BetterUIPlugin.sharedStringBuilder.Clear();

                bool isFirst = true;
                foreach ((float val, string part) in tupleList)
                {
                    if (!isFirst){BetterUIPlugin.sharedStringBuilder.Append("\n");}
                    isFirst = false;
                    
                    BetterUIPlugin.sharedStringBuilder.Append(part);
                    
                    switch(mode)
                    {
                        case 0:
                            BetterUIPlugin.sharedStringBuilder.Append(val.ToString("N0"));
                            break;
                        case 1: case 4:
                            BetterUIPlugin.sharedStringBuilder.Append(((val / total)*100).ToString("N0"));
                            BetterUIPlugin.sharedStringBuilder.Append(" %");
                            break;
                        case 2: case 3:
                            BetterUIPlugin.sharedStringBuilder.Append(val.ToString("N0"));
                            break;

                    }

                }
                
                textMesh.SetText(BetterUIPlugin.sharedStringBuilder);
            }


            if (chatBox != null && DPSMeterPanel != null) DPSMeterPanel.gameObject.SetActive(!chatBox.showInput);
        }

        public static void ChatBox_Awake(Action<RoR2.UI.ChatBox> orig, RoR2.UI.ChatBox self)
        {
            orig(self);
            chatBox = self;
        }
        public static void DamageDealtMessage_ClientDamageNotified(Action<DamageDealtMessage> orig, DamageDealtMessage dmgMsg)
        {
            
            orig(dmgMsg);

            CharacterMaster localMaster = LocalUserManager.GetFirstLocalUser().cachedMasterController.master;

            
            

            if (dmgMsg.attacker && dmgMsg.victim) 
            {
                var victimBody = dmgMsg.victim.gameObject.GetComponent<CharacterBody>();
                if (victimBody && victimBody.teamComponent.teamIndex != TeamIndex.Player)
                {

                    foreach (PlayerCharacterMasterController playerCharacterMaster in PlayerCharacterMasterController.instances)
                    {
                        if (dmgMsg.attacker == playerCharacterMaster.master.GetBodyObject()){

                            var userName = playerCharacterMaster.GetDisplayName();                                    //gets user name
                            playerLog log;
                          
                            if (userLogs.TryGetValue(userName, out log))
                            {
                                log.updatePlayerDamages(dmgMsg.damage);
                                log.playerDamageLog.Enqueue(new DamageLog(dmgMsg.damage));

                            }
                        }
                        else
                        {
                            var attackerBody = dmgMsg.attacker.GetComponent<CharacterBody>();
                            if (attackerBody && attackerBody.master && attackerBody.master.minionOwnership && attackerBody.master.minionOwnership.ownerMasterId == playerCharacterMaster.master.netId)
                            {
                                var userName = playerCharacterMaster.GetDisplayName();                                    //gets user name
                                playerLog log;
            
                                if (userLogs.TryGetValue(userName, out log))
                                {
                                    log.updateMinionDamages(dmgMsg.damage);
                                    log.minionDamageLog.Enqueue(new DamageLog(dmgMsg.damage));
                                }


                            }
                        }
                    }
                }
            }
        }



        private static void onHUDAwake(HUD self)
        {
            int playerCount = 0;
            userLogs = new Dictionary<string, playerLog>();
            foreach (PlayerCharacterMasterController playerCharacterMaster in PlayerCharacterMasterController.instances)
            {

                //var className = playerCharacterMaster.master.GetBodyObject().gameObject.GetComponent<CharacterBody>().GetDisplayName();
                var className = "";

                var classLog = new Queue<DamageLog>();
                var minionLog = new Queue<DamageLog>();



                var logEntry = new playerLog(className,classLog, minionLog);

                
                userLogs.Add(playerCharacterMaster.GetDisplayName(),logEntry);

                playerCount += 1;

            }

            if (DPSMeterPanel == null)
            {

                DPSMeterPanel = new GameObject("DPSMeterPanel");
                RectTransform rectTransform = DPSMeterPanel.AddComponent<RectTransform>();

                DPSMeterPanel.transform.SetParent(BetterUIPlugin.hud.mainContainer.transform);
                DPSMeterPanel.transform.SetAsFirstSibling();



                GameObject DPSMeterText = new GameObject("DPSMeterText");
                RectTransform rectTransform2 = DPSMeterText.AddComponent<RectTransform>();
                textMesh = DPSMeterText.AddComponent<HGTextMeshProUGUI>();


                DPSMeterText.transform.SetParent(DPSMeterPanel.transform);
                float scaling = (ConfigManager.DPSMeterWindowSize.Value.y * (playerCount - 1));
                Vector2 newSize = new Vector2(ConfigManager.DPSMeterWindowSize.Value.x,ConfigManager.DPSMeterWindowSize.Value.y * playerCount);
                Vector2 newPos = new Vector2(ConfigManager.DPSMeterWindowPosition.Value.x * 12,ConfigManager.DPSMeterWindowPosition.Value.y + scaling);
                Vector3 newEulerAngle = new Vector3( ConfigManager.DPSMeterWindowAngle.Value.x,  -ConfigManager.DPSMeterWindowAngle.Value.y, ConfigManager.DPSMeterWindowAngle.Value.z);

                rectTransform.localPosition = Vector3.zero;
                rectTransform.anchorMin = ConfigManager.DPSMeterWindowAnchorMin.Value;
                rectTransform.anchorMax = ConfigManager.DPSMeterWindowAnchorMax.Value;
                rectTransform.localScale = Vector3.one;
                rectTransform.pivot = ConfigManager.DPSMeterWindowPivot.Value;
                rectTransform.sizeDelta = newSize;
                rectTransform.anchoredPosition = newPos;
                rectTransform.eulerAngles = newEulerAngle;


                DPSMeterPanel.transform.SetParent(BetterUIPlugin.hud.mainUIPanel.transform);

                rectTransform2.localPosition = Vector3.zero;
                rectTransform2.anchorMin = Vector2.zero;
                rectTransform2.anchorMax = Vector2.one;
                rectTransform2.localScale = Vector3.one;
                rectTransform2.sizeDelta = new Vector2(-12, -12);
                rectTransform2.anchoredPosition = Vector2.zero;

                if (ConfigManager.DPSMeterWindowBackground.Value)
                {
                    Image image = DPSMeterPanel.AddComponent<Image>();
                    Image copyImage = BetterUIPlugin.hud.itemInventoryDisplay.gameObject.GetComponent<Image>();
                    image.sprite = copyImage.sprite;
                    image.color = copyImage.color;
                    image.type = Image.Type.Sliced;

                }

                textMesh.enableAutoSizing = true;
                textMesh.fontSizeMax = 256;
                textMesh.faceColor = Color.white;
                textMesh.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            }


        }
    }
}
