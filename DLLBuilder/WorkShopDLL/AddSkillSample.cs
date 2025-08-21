using System.Collections.Generic;

public class SampleTrainingAdder : IWorkshopTrainingModifier
{
    public List<string> WorkShopModifyTraining(List<string> trainingList)
    {
        trainingList.AddRange(TestSkillBuilder.skillNames);
        return trainingList;
    }
}

public class TestSkillBuilder : SkillFactory
{
    public static List<string> skillNames;
    public override void BuildSkill()
    {
        List<Skill> SkillList = new List<Skill>();
        skillNames = new List<string>();
        // 戮魂诀
        SkillList.Add(new EffectExcecuteSkillPro("戮魂", Quality.Rare)
        {
            DamageRate = 2.0f, // 伤害倍率
            ManaCost = 2 * 400, // 内力消耗
            CD = 2, // 当前冷却时间
            Discription = "造成强力伤害", // 技能描述
            Preempt = true,
            AttributeSelector = Skill =>
            {
                Skill.BaseExecute();
            }
        });
        // 枯荣咒
        SkillList.Add(new EffectExcecuteSkillPro("枯荣", Quality.Rare)
        {
            DamageRate = 1f, // 伤害倍率
            ManaCost = 2 * 300, // 内力消耗
            CD = 2, // 当前冷却时间
            Discription = "为对手附上3层随机【毒】", // 技能描述
            Preempt = false,
            AttributeSelector = Skill =>
            {
 
                for (int i = 0; i < 3; i++)
                {
                    PosionEffect poisonEffect = PosionFactory.RandomToxin();
                    poisonEffect.Intensity = 1;
                    Skill.Loader.GetOpponent().AddEffect(poisonEffect);
                }
                Skill.BaseExecute();
            }
        });
        // 夺元吸炁
        SkillList.Add(new EffectExcecuteSkillPro("夺元", Quality.Uncommon)
        {
            DamageRate = 1.2f, // 伤害倍率
            ManaCost = 2 * 250, // 内力消耗
            CD = 2, // 当前冷却时间
            Discription = "吸收对手300点内力并转为护盾", // 技能描述
            Preempt = false,
            AttributeSelector = Skill =>
            {
 
                int shieldValue = 300;
                Skill.Loader.ModifyArmor(shieldValue);
                Skill.Loader.GetOpponent().ConsumeMana(300);
                Skill.BaseExecute();
            }
        });
        // 返阳
        SkillList.Add(new EffectExcecuteSkillPro("返阳", Quality.Rare)
        {
            DamageRate = 0f, // 伤害倍率
            ManaCost = 2 * 300, // 内力消耗
            CD = 3, // 当前冷却时间
            Discription = "回血技能回复20%血量", // 技能描述
            Preempt = false,
            NoDamage = true,
            AttributeSelector = Skill =>
            {
                Skill.BaseExecute();
                int maxHealth = Skill.Loader.Player.AttributePart.getMaxHealth();
                int recoveryAmount = (int)(maxHealth * 0.20f);
                Skill.Loader.RecoverHealth(recoveryAmount);
            }
        });
        foreach (Skill skill in SkillList)
        {
            skillNames.Add(skill.Name);
            skill.AppearWeight = 1;//比常规招式，出场概率增加100%
        }
        SkillBuilder.SkillList.AddRange(SkillList);
    }
}
