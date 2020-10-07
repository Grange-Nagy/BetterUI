﻿using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace BetterUI
{
    class AdvancedIcons : BetterUI.ModComponent
    {
        public AdvancedIcons(BetterUI mod) : base(mod) { }
        public bool EquipmentIconDirty = true;
        public bool SkillIconDirty = true;
        public GenericSkill lastSkill;
        public EquipmentDef lastEquipment;
        List<ProcCoefficientCatalog.ProcCoefficientInfo> procCoefficientInfos;
        Inventory inventory;
        CharacterBody targetbody;

        Dictionary<string, float> skillCooldowns = new Dictionary<string, float>();
        internal void hook_SetItemIndex(On.RoR2.UI.ItemIcon.orig_SetItemIndex orig, RoR2.UI.ItemIcon self, ItemIndex itemIndex, int itemCount)
        {
            orig(self, itemIndex, itemCount);

            self.tooltipProvider.bodyToken = ItemCatalog.GetItemDef(itemIndex).descriptionToken;
        }

        internal override void Hook()
        {
            if (mod.config.AdvancedIconsSkillShowProcCoefficient.Value ||
                mod.config.AdvancedIconsSkillCalculateSkillProcEffects.Value ||
                mod.config.AdvancedIconsSkillShowBaseCooldown.Value ||
                mod.config.AdvancedIconsEquipementShowCalculatedCooldown.Value)
            {
                On.RoR2.UI.LoadoutPanelController.Row.AddButton += hook_LoadoutPanelController_Row_AddButton;
                On.RoR2.UI.SkillIcon.Update += hook_SkillIcon_Update;
                On.RoR2.CharacterMaster.OnInventoryChanged += hook_CharacterMaster_OnInventoryChanged;
            }
            if (mod.config.AdvancedIconsItemAdvancedDescriptions.Value)
            {
                On.RoR2.UI.ItemIcon.SetItemIndex += hook_SetItemIndex;
            }
            if (mod.config.AdvancedIconsEquipementAdvancedDescriptions.Value ||
                mod.config.AdvancedIconsEquipementShowBaseCooldown.Value ||
                mod.config.AdvancedIconsEquipementShowCalculatedCooldown.Value)
            {
                On.RoR2.UI.EquipmentIcon.Update += hook_EquipmentIcon_Update;
            }
        }

        private void hook_CharacterMaster_OnInventoryChanged(On.RoR2.CharacterMaster.orig_OnInventoryChanged orig, CharacterMaster self)
        {
            orig(self);
            this.SkillIconDirty = true;
            this.EquipmentIconDirty = true;
        }

        internal override void Start()
        {
            foreach(var skill in RoR2.Skills.SkillCatalog.allSkillDefs)
            {
                if(skill.baseRechargeInterval>0 && skill.requiredStock > 0)
                {
                    skillCooldowns[skill.skillNameToken] = skill.baseRechargeInterval;
                }
            }
        }

        internal void hook_LoadoutPanelController_Row_AddButton(On.RoR2.UI.LoadoutPanelController.Row.orig_AddButton orig, object self, LoadoutPanelController owner, Sprite icon, string titleToken, string bodyToken, Color tooltipColor, UnityEngine.Events.UnityAction callback, string unlockableName, ViewablesCatalog.Node viewableNode, bool isWIP = false)
        {
            orig(self, owner, icon, titleToken, bodyToken, tooltipColor, callback, unlockableName, viewableNode, isWIP);

            LoadoutPanelController.Row selfRow = (LoadoutPanelController.Row) self;
            UserProfile userProfile = selfRow.userProfile;
            if (mod.config.AdvancedIconsSkillShowProcCoefficient.Value || mod.config.AdvancedIconsSkillShowBaseCooldown.Value)
            {
                if (userProfile != null && userProfile.HasUnlockable(unlockableName))
                {
                    BetterUI.sharedStringBuilder.Clear();
                    BetterUI.sharedStringBuilder.Append(Language.GetString(bodyToken));
                    if (mod.config.AdvancedIconsSkillShowBaseCooldown.Value && skillCooldowns.ContainsKey(titleToken))
                    {
                        BetterUI.sharedStringBuilder.Append("\n\nCooldown: <style=cIsDamage>");
                        BetterUI.sharedStringBuilder.Append(skillCooldowns[titleToken]);
                        BetterUI.sharedStringBuilder.Append("</style> seconds");
                    }

                    if (mod.config.AdvancedIconsSkillShowProcCoefficient.Value)
                    {
                        List<ProcCoefficientCatalog.ProcCoefficientInfo> procCoefficientInfos = ProcCoefficientCatalog.GetProcCoefficientInfo(titleToken);

                        if (procCoefficientInfos != null)
                        {
                            foreach (var info in procCoefficientInfos)
                            {
                                BetterUI.sharedStringBuilder.Append("\n\n<size=110%>");
                                BetterUI.sharedStringBuilder.Append(info.name);
                                BetterUI.sharedStringBuilder.Append(":</size>");
                                if (mod.config.AdvancedIconsSkillShowProcCoefficient.Value)
                                {
                                    BetterUI.sharedStringBuilder.Append("\n <style=cIsUtility>Proc Coefficient: ");
                                    BetterUI.sharedStringBuilder.Append(info.procCoefficient);
                                    BetterUI.sharedStringBuilder.Append("</style>");
                                }
                            }

                            
                        }
                    }
                    TooltipProvider tooltipProvider = selfRow.buttons[selfRow.buttons.Count - 1].GetComponent<TooltipProvider>();
                    if (tooltipProvider != null)
                    {
                        tooltipProvider.overrideBodyText = BetterUI.sharedStringBuilder.ToString();
                    }
                }
            }
        }
        internal void hook_SkillIcon_Update(On.RoR2.UI.SkillIcon.orig_Update orig, SkillIcon self)
        {
            orig(self);

            if (self.targetSkill && self.targetSkill != this.lastSkill && this.SkillIconDirty)
            {
                this.lastSkill = self.targetSkill;
                this.SkillIconDirty = false;
                BetterUI.sharedStringBuilder.Clear();
                BetterUI.sharedStringBuilder.Append(Language.GetString(self.targetSkill.skillDescriptionToken));
                if (mod.config.AdvancedIconsSkillShowBaseCooldown.Value || mod.config.AdvancedIconsSkillShowCalculatedCooldown.Value)
                {
                    BetterUI.sharedStringBuilder.Append("\n");
                }
                if (mod.config.AdvancedIconsSkillShowBaseCooldown.Value)
                {
                    BetterUI.sharedStringBuilder.Append("\nBase Cooldown: <style=cIsDamage>");
                    BetterUI.sharedStringBuilder.Append(self.targetSkill.baseRechargeInterval);
                    BetterUI.sharedStringBuilder.Append("</style> seconds");
                }
                if (mod.config.AdvancedIconsSkillShowCalculatedCooldown.Value && self.targetSkill.baseRechargeInterval > self.targetSkill.finalRechargeInterval)
                {
                    BetterUI.sharedStringBuilder.Append("\nEffective Cooldown: <style=cIsHealing>");
                    BetterUI.sharedStringBuilder.Append(self.targetSkill.finalRechargeInterval);
                    BetterUI.sharedStringBuilder.Append("</style> seconds");
                }

                if (mod.config.AdvancedIconsSkillShowProcCoefficient.Value || mod.config.AdvancedIconsSkillCalculateSkillProcEffects.Value)
                {
                    procCoefficientInfos = ProcCoefficientCatalog.GetProcCoefficientInfo(self.targetSkill.skillDef.skillNameToken);

                    if (procCoefficientInfos != null)
                    {
                        foreach (var info in procCoefficientInfos)
                        {
                            BetterUI.sharedStringBuilder.Append("\n\n<size=110%>");
                            BetterUI.sharedStringBuilder.Append("info.name");
                            BetterUI.sharedStringBuilder.Append("</size>");
                            if (mod.config.AdvancedIconsSkillShowProcCoefficient.Value)
                            {
                                BetterUI.sharedStringBuilder.Append("\n <style=cIsUtility>Proc Coefficient: ");
                                BetterUI.sharedStringBuilder.Append(info.procCoefficient);
                                BetterUI.sharedStringBuilder.Append("</style>");
                            }
                            if (info.procCoefficient > 0 && mod.config.AdvancedIconsSkillCalculateSkillProcEffects.Value)
                            {
                                foreach (var item in ProcItemsCatalog.GetAllItems())
                                {
                                    int stacks = self.targetSkill.characterBody.inventory.itemStacks[(int)item.Key];
                                    if (stacks > 0)
                                    {
                                        ItemDef itemDef = ItemCatalog.GetItemDef(item.Key);
                                        BetterUI.sharedStringBuilder.Append("\n  ");
                                        BetterUI.sharedStringBuilder.Append(Language.GetString(itemDef.nameToken));
                                        BetterUI.sharedStringBuilder.Append(": ");
                                        BetterUI.sharedStringBuilder.Append(item.Value.GetOutputString(stacks, self.targetSkill.characterBody.master.luck, info.procCoefficient));
                                    }
                                }
                            }
                        }
                    }
                }

                self.tooltipProvider.overrideBodyText = BetterUI.sharedStringBuilder.ToString();
            }

            if (mod.config.AdvancedIconsSkillShowCooldownStacks.Value && self.targetSkill && self.targetSkill.cooldownRemaining > 0)
            {
                BetterUI.sharedStringBuilder.Clear();
                BetterUI.sharedStringBuilder.AppendInt(Mathf.CeilToInt(self.targetSkill.cooldownRemaining), 0U, uint.MaxValue);
                self.cooldownText.SetText(BetterUI.sharedStringBuilder);
                self.cooldownText.gameObject.SetActive(true);
            }
        }

        internal void hook_EquipmentIcon_Update(On.RoR2.UI.EquipmentIcon.orig_Update orig, EquipmentIcon self)
        {
            orig(self);
            if ((mod.config.AdvancedIconsEquipementAdvancedDescriptions.Value || 
                mod.config.AdvancedIconsEquipementShowBaseCooldown.Value || 
                mod.config.AdvancedIconsEquipementShowCalculatedCooldown.Value) && 
                (self.currentDisplayData.equipmentDef != this.lastEquipment || this.EquipmentIconDirty) &&
                self.currentDisplayData.hasEquipment && self.tooltipProvider)
            {
                lastEquipment = self.currentDisplayData.equipmentDef;
                this.EquipmentIconDirty = false;
                BetterUI.sharedStringBuilder.Clear();
                BetterUI.sharedStringBuilder.Append(Language.GetString(mod.config.AdvancedIconsEquipementAdvancedDescriptions.Value ? self.currentDisplayData.equipmentDef.descriptionToken : self.currentDisplayData.equipmentDef.pickupToken));
                if(mod.config.AdvancedIconsEquipementShowBaseCooldown.Value || mod.config.AdvancedIconsEquipementShowCalculatedCooldown.Value)
                {
                    BetterUI.sharedStringBuilder.Append("\n");
                }
                if (mod.config.AdvancedIconsEquipementShowBaseCooldown.Value)
                {
                    BetterUI.sharedStringBuilder.Append("\nBase Cooldown: <style=cIsDamage>");
                    BetterUI.sharedStringBuilder.Append(self.currentDisplayData.equipmentDef.cooldown);
                    BetterUI.sharedStringBuilder.Append("</style> seconds");
                }
                if (mod.config.AdvancedIconsEquipementShowCalculatedCooldown.Value)
                {
                    inventory = self.targetInventory;
                    if (!inventory && mod.HUD.targetBodyObject)
                    {
                        targetbody = mod.HUD.targetBodyObject.GetComponent<CharacterBody>();
                        if (targetbody)
                        {
                            inventory = targetbody.inventory;
                        }
                    }
                    if (inventory)
                    {
                        float reduction = (float)Math.Pow(0.85, inventory.itemStacks[(int)ItemIndex.EquipmentMagazine]);
                        if (inventory.itemStacks[(int)ItemIndex.AutoCastEquipment] > 0)
                        {
                            reduction *= 0.5f * (float)Math.Pow(0.85, inventory.itemStacks[(int)ItemIndex.AutoCastEquipment] - 1);
                        }
                        if (reduction < 1)
                        {
                            BetterUI.sharedStringBuilder.Append("\nEffective Cooldown: <style=cIsHealing>");
                            BetterUI.sharedStringBuilder.Append((self.currentDisplayData.equipmentDef.cooldown * reduction).ToString("0.###"));
                            BetterUI.sharedStringBuilder.Append("</style> seconds");
                        }
                    }
                }
                

                self.tooltipProvider.overrideBodyText = Util.sharedStringBuilder.ToString();
            }

            if (mod.config.AdvancedIconsEquipementShowCooldownStacks.Value && self.cooldownText && self.currentDisplayData.cooldownValue > 0)
            {
                self.cooldownText.gameObject.SetActive(true);
            }
        }
    }
}
