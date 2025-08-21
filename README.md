# 🔧 创意工坊招式接入教程（IWorkshopTrainingModifier + SkillFactory）

```
MiniMartialWorld-MiniMartialWorld-CustomSkill
├─ DLLBuilder/           # 你的 C# Mod 工程（编译成 .dll）
├─ ContentSample/        # 创意工坊示例（放置编译好的 .dll 与预览图）
└─ README.md             # 本说明（你正在看）
```

本教程讲解如何编写并打包**自定义招式并将其放到传武堂修炼池子**，并以「示例解析」的统一格式给出完整代码与注释。

---

## 快速开始（3 步）
1. 在 `DLLBuilder/` 新建或打开你的 C# 工程（Unity 兼容的 .NET 版本），参照模板添加依赖引用。
2. 在工程中创建你的事件脚本：实现 `IWorkshopTrainingModifier`（传武堂修炼导入）和 `SkillFactory`（技能工厂）。
3. 编译生成 `YourMod.dll`，与预览图一起放入 `ContentSample/`，添加对应图片，上传创意工坊即可加载。


## 示例解析

### 1) 训练列表扩展：`SampleTrainingAdder`

```csharp
public class SampleTrainingAdder : IWorkshopTrainingModifier
{
    public List<string> WorkShopModifyTraining(List<string> trainingList)
    {
        trainingList.AddRange(TestSkillBuilder.skillNames);
        return trainingList;
    }
}
```

- **作用**：把 `TestSkillBuilder` 中收集的 **技能名**（`skillNames`）追加到现有的 `trainingList`，使这些招式可以出现在**传武堂**界面或逻辑中（每次刷新传武堂的时候会重新加载）。  
- **注意**：这里追加的是**字符串名字**；真正的技能实体在 `SkillBuilder.SkillList` 中由 `SkillFactory` 注册（见下）。
### 2) 技能工厂：`TestSkillBuilder`（BuildSkill）

```csharp
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

        foreach (Skill skill in SkillList)
        {
            skillNames.Add(skill.Name);
            skill.AppearWeight = 1;//比常规招式，出场概率增加100%
        }
        SkillBuilder.SkillList.AddRange(SkillList);
    }
}
```

#### 出现权重：`skill.AppearWeight = 1`

- 示例注释“**比常规招式，出场概率增加100%**”：表示相对于默认权重，**在同类抽取时给予额外权重**。  
- 实际权重基线由游戏侧定义；此处的 `1` 作为**增量**或**附加**，用于提高这些招式的出现率。

#### 注册与训练列表的衔接

- `SkillBuilder.SkillList.AddRange(SkillList)`：把**实体技能**注册到全局技能池。  
- `skillNames.Add(skill.Name)`：把名字记录到静态列表；随后 `SampleTrainingAdder` 使用 `AddRange(skillNames)` 把名字写入训练池，**两端打通**。

---

 
## 关键字段速查（统一约定）

- **Preempt**：战斗开始时可以释放（先制释放）。
- **IsNoCost**：释放该技能后**不占用本回合行动**，可继续释放其他技能（相当于“免费动作”）。
- **NoDamage**：该技能本体不直接造成伤害（常配合治疗/增益/固伤等）。
- **IsTimeLimit / TimeUpperLimit**：限次技能与次数上限。
- **CD**：冷却（数值越大，下次可用越晚）；
- **EnableSelector(Skill s)**：是否允许释放的判定器（如“护盾 ≥ 300 才能用”）。
- **AttributeSelector(Skill s)**：技能主逻辑（增减属性、固定伤害、施加效果等），常用 `s.BaseExecute()` 执行基础结算。
- **AppearWeight/NoWeight**：出现在随机池的权重附加或取消权重（即不会在传武堂中出现）。

---
## HopeForImmortalFactory（多功能治疗/固伤/护盾/重置）

### 用途与要点
- **多功能实用包**：满血治疗（`悬壶截脉` / `IsNoCost` + `Preempt`）、按**自身上限生命固定伤害**（`血绫绞喉`，`attack(..., FixedDamage:true)`）、护盾/治疗（`抱元`、`玄罡护体`）、**内力与护盾联动**（`归元真息`）。
- 多处使用 `Preempt`（开场先手技能）与 `IsNoCost`（不占行动，可连放）强化节奏与爆发。

### 关键点速读
- `Preempt=true`：如 `悬壶截脉/血绫绞喉/抱元/沾衣/玄罡护体` 等**战斗开始即可释放**。
- `IsNoCost=true`：如 `悬壶截脉/抱元/玄罡护体` —— **释放后还能继续出其他技能**。
- 固伤范式：`Loader.attack(上限生命×比例, FixedDamage:true)` —— **无视防御与增减伤**。
- 资源联动：`归元真息` 将**回复的内力**等量转为**护盾**，兼顾续航与保命。

### 原始源码

```csharp
﻿using System.Collections.Generic;
using System.Linq;

public class HopeForImmortalFactory : SkillFactory
{
    public static List<string> skillNames;

    public override void BuildSkill()
    {
        List<Skill> SkillList = new List<Skill>();
        skillNames = new List<string>();

        // 悬壶截脉
        SkillList.Add(new EffectExcecuteSkillPro("悬壶截脉", Quality.Rare)
        {
            DamageRate = 0f, // 无伤害
            ManaCost = 2 * 300, // 内力消耗
            CD = 0, // 冷却时间
            Discription = "恢复全部血量", // 技能描述
            Preempt = true,
            NoDamage = true,
            IsNoCost = true,
            IsTimeLimit = true,
            TimeUpperLimit = 1,
            AttributeSelector = Skill =>
            {
                Skill.BaseExecute();
                Skill.Loader.RecoverHealth(Skill.Loader.Player.AttributePart.getMaxHealth());
            }
        });
        // 血绫绞喉
        SkillList.Add(new EffectExcecuteSkillPro("血绫绞喉", Quality.Uncommon)
        {
            DamageRate = 0f, // 固定伤害
            ManaCost = 2 * 150, // 内力消耗
            CD = 2, // 冷却时间
            Discription = "对敌人造成等同于自身血量上限25%的固定伤害", // 技能描述
            Preempt = true,
            NoDamage = true,
            IsTimeLimit = true,
            TimeUpperLimit = 3,
            AttributeSelector = Skill =>
            {
                Skill.BaseExecute();
                int damage = (int)(Skill.Loader.Player.AttributePart.getMaxHealth() * 0.25f);
                Skill.Loader.attack(damage, FixedDamage: true);
            }
        });

        // 抱元
        SkillList.Add(new EffectExcecuteSkillPro("抱元", Quality.Rare)
        {
            DamageRate = 0f, // 无伤害
            ManaCost = 2 * 450, // 内力消耗
            CD = 3, // 冷却时间
            Discription = "恢复 (根骨x2)% 的血量和内力", // 技能描述
            NoDamage = true,
            Preempt = true,
            AttributeSelector = Skill =>
            {
                Skill.BaseExecute();
                int recoveryAmount = (int)(Skill.Loader.Player.AttributePart.Physique * 2);
                Skill.Loader.RecoverHealth(recoveryAmount * Skill.Loader.Player.AttributePart.getMaxHealth() / 100);
                Skill.Loader.RecoverMana(recoveryAmount * Skill.Loader.Player.AttributePart.getMaxMana() / 100);
            }
        });

        // 沾衣
        SkillList.Add(new EffectExcecuteSkillPro("沾衣", Quality.Rare)
        {
            DamageRate = 0f, // 无伤害
            ManaCost = 2 * 200, // 内力消耗
            CD = 3, // 冷却时间
            Discription = "获得两层【抵御】", // 技能描述
            NoDamage = true,
            IsTimeLimit = true,
            TimeUpperLimit = 3,
            Preempt = true,
            AttributeSelector = Skill =>
            {
                Skill.BaseExecute();
                Effect shieldEffect = new DiYu(2);
                Skill.Loader.AddEffect(shieldEffect);
            }
        });





        SkillList.Add(new EffectExcecuteSkillPro("玄罡护体", Quality.Rare)
        {
            DamageRate = 0f, // 无伤害
            ManaCost = 2 * 500, // 内力消耗
            CD = 2, // 冷却时间
            Discription = "获得当前血量最大值30%的护盾", // 技能描述
            NoDamage = true,
            Preempt = true, // 急速
            IsTimeLimit = true,
            IsNoCost = true,
            TimeUpperLimit = 2, // 可使用2次
            AttributeSelector = Skill =>
            {
                Skill.BaseExecute();
                int shieldAmount = (int)(Skill.Loader.Player.AttributePart.getMaxHealth() * 0.30f);
                Skill.Loader.ModifyArmor(shieldAmount);
            }
        });



        // 破元劲
        SkillList.Add(new EffectExcecuteSkillPro("破元劲", Quality.Epic)
        {
            DamageRate = 0f, // 固定伤害
            ManaCost = 2 * 1200, // 内力消耗
            CD = 0, // 冷却时间
            Discription = "对敌人造成等同于自身血量上限60%的固定伤害", // 技能描述
            Preempt = true, // 急速
            NoDamage = true,
            IsTimeLimit = true, // 次数限制
            TimeUpperLimit = 1, // 限制1次使用
            AttributeSelector = Skill =>
            {
                Skill.BaseExecute();
                int damage = (int)(Skill.Loader.Player.AttributePart.getMaxHealth() * 0.60f);
                Skill.Loader.attack(damage, FixedDamage: true);
            }
        });

        // 极略
        SkillList.Add(new EffectExcecuteSkillPro("极略", Quality.Epic)
        {
            DamageRate = 0f, // 无伤害
            ManaCost = 2 * 0, // 无内力消耗
            CD = 0, // 冷却时间
            Discription = "重置所有招式的冷却", // 技能描述
            NoDamage = true,
            IsNoCost = true, // 该技能无消耗
            IsTimeLimit = true, // 限制使用次数
            TimeUpperLimit = 1, // 仅能使用1次
            Preempt = true, // 急速
            AttributeSelector = Skill =>
            {
                Skill.BaseExecute();
                foreach (Skill s in Skill.Loader.Skills)
                {
                    s.CurrentCooldown = 0; // 重置所有技能的冷却时间
                }
            }
        });

        SkillList.Add(new EffectExcecuteSkillPro("除瘴术", Quality.Epic)
        {
            DamageRate = 0f, // 无伤害
            ManaCost = 2 * 200, // 内力消耗
            CD = 2, // 冷却时间
            Discription = "解除所有【毒】，获得解除【毒】层数*100的护盾", // 技能描述
            NoDamage = true,
            IsTimeLimit = true, // 限制使用次数
            TimeUpperLimit = 2, // 仅能使用2次
            Preempt = true, // 急速
            AttributeSelector = Skill =>
            {
                Skill.BaseExecute();
                int poisonLayersRemoved = 0;

                // 遍历所有效果，寻找毒效果并移除
                foreach (Effect effect in Skill.Loader.Effects.ToList())
                {
                    if (effect is PosionEffect poisonEffect)
                    {
                        poisonLayersRemoved += poisonEffect.Intensity; // 统计毒的层数
                        poisonEffect.ModifyIntensity(-poisonEffect.Intensity);// 解除毒
                    }
                }
                // 计算护盾值 (每层毒提供100护盾)
                int shieldAmount = poisonLayersRemoved * 100;
                Skill.Loader.ModifyArmor(shieldAmount);
            }
        });

        // 归元真息
        SkillList.Add(new EffectExcecuteSkillPro("归元真息", Quality.Epic)
        {
            DamageRate = 0f, // 无伤害
            ManaCost = 2 * 0, // 无内力消耗
            CD = 3, // 冷却时间
            IsTimeLimit = true, // 限制使用次数
            TimeUpperLimit = 5, // 仅能使用2次
            Discription = "调息归元，恢复20%最大内力，并获得等量护盾。", // 技能描述
            NoDamage = true,
            Preempt = true, // 急速
            AttributeSelector = Skill =>
            {
                Skill.BaseExecute();
                int manaRecovery = (int)(Skill.Loader.Player.AttributePart.getMaxMana() * 0.20f);

                Skill.Loader.RecoverMana(manaRecovery); // 恢复内力
                Skill.Loader.ModifyArmor(manaRecovery); // 获得等量护盾
            }
        });



        // 天地同寿
        SkillList.Add(new EffectExcecuteSkillPro("天地同寿", Quality.Epic)
        {
            DamageRate = 0f, // 无伤害
            ManaCost = 2 * 1200, // 无内力消耗
            CD = 2, // 冷却时间
            Discription = "自身生命值降至1点，并对敌人造成等量伤害", // 技能描述
            NoDamage = true,
            AttributeSelector = Skill =>
            {
                Skill.BaseExecute();
                int selfHealth = Skill.Loader.Player.AttributePart.Health;
                int damageToEnemy = selfHealth - 1; // 计算即将损失的血量

                Skill.Loader.Player.AttributePart.Health = 1; // 将自身血量降至1
                Skill.Loader.attack(damageToEnemy, FixedDamage: true);
            }
        });




        foreach (Skill skill in SkillList)
        {
            skillNames.Add(skill.Name);

        }
 
        SkillBuilder.SkillList.AddRange(SkillList);
    }
}


```


---

## ArmorSkillBuilder（护盾流的转化与爆发）

### 用途与要点
- **护盾系核心**：以护盾为资源，通过“有盾增强伤害”“消耗盾换倍率”“按盾量滚雪球”等形成**防御→进攻**转化。
- `EnableSelector` 控制可用性：如“**护盾 ≥ 300 才能释放**”。

### 原始源码

```csharp
﻿using System;
using System.Collections.Generic;
using System.Linq;

public class ArmorSkillBuilder : SkillFactory
{
    public static List<string> SkillNames;

    public static List<string> LibrarySkillNames;
    public override void BuildSkill()
    {
        List<Skill> skills = new List<Skill>();
        SkillNames = new List<string>();
        LibrarySkillNames = new List<string>();
        BasicSkill SampleSkill = new EffectExcecuteSkillPro("绵掌", Quality.Rare)
        {
            DamageRate = 1.0f, // 伤害倍率
            ManaCost = 2 * 200, // 内力消耗
            CD = 2, // 当前冷却时间
            Preempt = true,
            Discription = "获得100（受根骨加成）点护盾", // 技能描述
            // 设置技能的自定义逻辑
            AttributeSelector = skillPro =>
            {
                skillPro.Loader.ModifyArmor(100 + (int)(skillPro.Loader.Player.AttributePart.Physique * 4));
                skillPro.BaseExecute(); // 调用基类的 Excecute 方法
            }
        };
        skills.Add(SampleSkill);
        LibrarySkillNames.Add(SampleSkill.Name);

        SampleSkill = new EffectExcecuteSkillPro("开山掌", Quality.Uncommon)
        {
            DamageRate = 1.2f, // 伤害倍率
            ManaCost = 2 * 80, // 内力消耗
            CD = 3, // 当前冷却时间
            Discription = "当角色存在护盾时，技能伤害提升100", // 技能描述
            Preempt = true,
            // 设置技能的自定义逻辑
            AttributeSelector = skillPro =>
            {
                if (skillPro.Loader.Armor > 0)
                {
                    skillPro.DamageRate += 1;
                    skillPro.BaseExecute(); // 调用基类的 Excecute 方法
                    skillPro.DamageRate -= 1;
                }
                else
                {
                    skillPro.BaseExecute(); // 调用基类的 Excecute 方法
                }
            }

        };
        skills.Add(SampleSkill);
        LibrarySkillNames.Add(SampleSkill.Name);


        SampleSkill = new EffectExcecuteSkillPro("持盾猛击", Quality.Rare)
        {
            DamageRate = 5.0f, // 伤害倍率
            ManaCost = 2 * 500, // 内力消耗
            CD = 3, // 当前冷却时间
            Discription = "消耗300点护盾", // 技能描述
            Preempt = true,
            AppearWeight = 1,
            EnableSelector = skill =>
            {
                // 假设这里判断技能是否可用的逻辑，例如基于某些条件判断
                return skill.BaseGetEnable() && skill.Loader.Armor >= 300;// 假设条件是玩家的魔法值足够
            },
            // 设置技能的自定义逻辑
            AttributeSelector = skillPro =>
            {
                skillPro.Loader.ModifyArmor(-300);
                skillPro.BaseExecute(); // 调用基类的 Excecute 方法
            }
        };
        skills.Add(SampleSkill);
        LibrarySkillNames.Add(SampleSkill.Name);


        SampleSkill = new EffectExcecuteSkillPro("强效加固", Quality.Epic)
        {
            DamageRate = 0f, // 伤害倍率
            ManaCost = 2 * 500, // 内力消耗
            CD = 3, // 当前冷却时间
            //Preempt = true,
            NoDamage = true,
            Discription = "获得当前护盾量20%的护盾(上限1800点)", // 技能描述
            // 设置技能的自定义逻辑
            AttributeSelector = skillPro =>
            {
                int Armor = skillPro.Loader.Armor / 5;
                skillPro.Loader.ModifyArmor(Math.Min(1800, Armor));
                skillPro.BaseExecute(); // 调用基类的 Excecute 方法
            }
        };
        skills.Add(SampleSkill);
        LibrarySkillNames.Add(SampleSkill.Name);






        SampleSkill = new EffectExcecuteSkillPro("金身百炼", Quality.Epic)
        {
            DamageRate = 0f, // 伤害倍率
            ManaCost = 2 * 300, // 内力消耗
            CD = 0, // 当前冷却时间
            Preempt = true,
            IsNoCost = true,
            IsTimeLimit = true,
            NoDamage = true,
            TimeUpperLimit = 1,
            LimitTime = 1,
            Discription = "获得当前护盾量50%的护盾", // 技能描述
            // 设置技能的自定义逻辑
            AttributeSelector = skillPro =>
            {
                int Armor = skillPro.Loader.Armor / 2;
                skillPro.Loader.ModifyArmor(Armor);
                skillPro.BaseExecute(); // 调用基类的 Excecute 方法
            }
        };
        skills.Add(SampleSkill);

        SampleSkill = new EffectExcecuteSkillPro("金钟罩", Quality.Epic)
        {
            DamageRate = 0f, // 伤害倍率
            ManaCost = 2 * 600, // 内力消耗
            CD = 5, // 当前冷却时间
            Preempt = true,
            NoDamage = true,
            Discription = "获得（防御x3+根骨x150）点护盾 ", // 技能描述
            // 设置技能的自定义逻辑
            AttributeSelector = skillPro =>
            {
                int Num = (int)(AttributeCalculator.GetBaseDef(skillPro.Loader.Player) * 3f + skillPro.Loader.Player.AttributePart.Physique * 150);
                skillPro.Loader.ModifyArmor(Num);
                skillPro.Continue(); // 调用基类的 Excecute 方法
            }
        };
        skills.Add(SampleSkill);

        LibrarySkillNames.Add(SampleSkill.Name);



        SampleSkill = new EffectExcecuteSkillPro("舍身猛击", Quality.Epic)
        {
            DamageRate = 1.8f, // 伤害倍率
            ManaCost = 2 * 700, // 内力消耗
            //Preempt = true,
            CD = 5, // 当前冷却时间
            Discription = "将所有护盾转化为额外伤害（受根骨加成）", // 技能描述
            // 设置技能的自定义逻辑
            AttributeSelector = skillPro =>
            {
                int Num = skillPro.Loader.Armor + (int)(skillPro.Loader.Player.AttributePart.Physique * 10);
                skillPro.Loader.ModifyArmor(-Num);
                skillPro.Loader.Player.AttributePart.Atk += (int)(Num * 0.18f);
                skillPro.BaseExecute(); // 调用基类的 Excecute 方法
                skillPro.Loader.Player.AttributePart.Atk -= (int)(Num * 0.18f);
            }
        };
        skills.Add(SampleSkill);


        foreach (Skill skill in skills)
        {
            SkillNames.Add(skill.Name);
        }
        SkillBuilder.SkillList.AddRange(skills);

    }
}

```

---

## ExtraPoisionBuilder（毒种类/层数的协同）

### 用途与要点
- **毒的“种类”与“层数”双系统**：通过增加毒的种类/层数驱动倍率、治疗、内力汲取等多种收益。
- 利用 `DescriptionSelector/Count` 做**越用越强**（如 `溃脉针`）。

### 关键点速读
- `溃脉针`：每次使用**+1 层随机毒**；描述随使用次数自增（成长技）。
- `腐骨劲`：按“敌方**毒的种类数**×0.6 倍”增伤。
- `搬血咒`：敌方每种毒 → 自身**回复上限生命 4%**（`Preempt` + 限次）。
- `噬功大法`：按“毒的种类数”**夺取对方内力并回复自身**。

### 原始源码（保持不变）

```csharp
﻿using System;
using System.Collections.Generic;
using System.Linq;

public class ExtraPoisionBuilder : SkillFactory
{
    public static List<string> SkillNames;

    public override void BuildSkill()
    {
        List<Skill> skills = new List<Skill>();
        SkillNames = new List<string>();

        // 溃脉针
        skills.Add(new EffectExcecuteSkillPro("溃脉针", Quality.Epic)
        {
            DamageRate = 1f, // 伤害倍率
            ManaCost = 2 * 300, // 内力消耗
            CD = 2, // 冷却时间
            AppearWeight = 1,
            DescriptionSelector = skillPro =>
            {
                return $"随机给敌人附上{1 + skillPro.Count}层随机【毒】,每次使用后会强化该招式";
            },
            AttributeSelector = skillPro =>
            {
                int num = 1 + skillPro.Count; // 获取技能使用次数
                for (int i = 0; i < num; i++)
                {
                    PosionEffect poisonEffect = PosionFactory.RandomToxin(); // 随机生成毒
                    poisonEffect.Intensity = 1;
                    skillPro.Loader.GetOpponent().AddEffect(poisonEffect); // 添加毒效果到敌人
                }

                skillPro.BaseExecute(); // 在最后执行基础操作

            }
        });

        // 腐骨劲
        skills.Add(new EffectExcecuteSkillPro("腐骨劲", Quality.Rare)
        {
            DamageRate = 1f, // 伤害倍率
            ManaCost = 2 * 300, // 内力消耗
            CD = 2, // 冷却时间
            Discription = "敌人每有一种【毒】，威力+60",
            AttributeSelector = skillPro =>
            {
                skillPro.BaseExecute(); // 调用基类的 Excecute 方法

                int poisonTypes = 0; // 统计毒的种类
                foreach (Effect effect in skillPro.Loader.GetOpponent().Effects)
                {
                    if (effect is PosionEffect poisonEffect)
                    {
                        poisonTypes++; // 统计不同的毒类型
                    }
                }
                skillPro.DamageRate += poisonTypes * 0.6f; // 每种毒增加60威力


                skillPro.BaseExecute(); // 执行基础技能
                skillPro.DamageRate -= poisonTypes * 0.6f;
            }
        });

        // 搬血咒
        skills.Add(new EffectExcecuteSkillPro("搬血咒", Quality.Epic)
        {
            DamageRate = 1f, // 无伤害
            ManaCost = 2 * 400, // 内力消耗
            CD = 2, // 冷却时间
            Discription = "敌人每有一种【毒】，恢复自身4%血量",
            Preempt = true, // 先制
            IsTimeLimit = true, // 次数限制
            TimeUpperLimit = 5, // 最多3次使用
            AttributeSelector = skillPro =>
            {
                int poisonTypes = 0; // 统计毒的种类
                foreach (Effect effect in skillPro.Loader.GetOpponent().Effects)
                {
                    if (effect is PosionEffect poisonEffect)
                    {
                        poisonTypes++; // 统计不同的毒类型
                    }
                }

                int recoveryAmount = (int)(poisonTypes * 0.04f * skillPro.Loader.Player.AttributePart.getMaxHealth()); // 每种毒恢复4%
                skillPro.Loader.RecoverHealth(recoveryAmount); // 恢复血量

                skillPro.BaseExecute(); // 执行基础技能
            }
        });

        // 摄魂引
        skills.Add(new EffectExcecuteSkillPro("摄魂引", Quality.Rare)
        {
            DamageRate = 2f, // 无伤害
            ManaCost = 2 * 350, // 内力消耗
            CD = 4, // 冷却时间
            Discription = "若敌人每有一层【幻毒】，恢复8%血量（最多恢复40%）",

            NoWeight = true,

            AttributeSelector = skillPro =>
            {
                int Layers = skillPro.Loader.GetEffectIntensityByName("幻毒");
                foreach (Effect effect in skillPro.Loader.GetOpponent().Effects)
                {
                    if (effect is PosionEffect poisonEffect && poisonEffect.Name == "幻毒")
                    {
                        Layers++;
                    }
                }

                int recoveryAmount = (Math.Min(Layers * 8, 40) * skillPro.Loader.Player.AttributePart.getMaxHealth() / 100); // 每层幻毒恢复8%血量，最多恢复40%
                skillPro.Loader.RecoverHealth(recoveryAmount); // 恢复血量
                skillPro.BaseExecute(); // 执行基础技能
            }
        });

        // 噬功毒经
        skills.Add(new EffectExcecuteSkillPro("噬功大法", Quality.Rare)
        {
            DamageRate = 1f, // 无伤害
            ManaCost = 2 * 0, // 内力消耗
            CD = 3, // 冷却时间
            Discription = "敌人每有一种【毒】，夺取对方5%内力",
            NoWeight = true,
            AttributeSelector = skillPro =>
            {
                int poisonTypes = 0;
                foreach (Effect effect in skillPro.Loader.GetOpponent().Effects)
                {
                    if (effect is PosionEffect poisonEffect)
                    {
                        poisonTypes++; // 统计毒的种类
                    }
                }

                float manaDrain = (int)(0.05f * skillPro.Loader.GetOpponent().Player.AttributePart.getMaxMana() * poisonTypes);
                manaDrain = skillPro.Loader.GetOpponent().ConsumeMana(manaDrain);
                skillPro.Loader.RecoverMana(manaDrain);
                skillPro.BaseExecute(); // 执行基础技能
            }
        });
        foreach (Skill skill in skills)
        {
            SkillNames.Add(skill.Name);
        }
        SkillBuilder.SkillList.AddRange(skills);
    }
}
```


---

## MedicineAdditionBuilder（医术缩放与汲取）

### 用途与要点
- **医术等级驱动的缩放**：治疗/固伤比例随 `Medicine.getLevel()` 成长，形成**技能-职业**耦合。
- 代表：`续脉通络`（双恢复按等级加成）、`蚀心咒`/`错经改穴`/`夺髓济生` 的**扣血/转伤/汲取治疗**。

### 关键点速读
- 恢复比例：`0.08 + 等级×0.005`（续脉通络）。
- 扣血比例：`基准 + 等级×附加`，如 `夺髓济生` = `8% + 等级×1%`；**恢复有上限（2000）**。
- 多数 `Preempt + NoDamage`：可在**战斗开始**即治疗/压血线。

### 原始源码（保持不变）

```csharp
﻿using System;
using System.Collections.Generic;
using System.Linq;

public class MedicineAdditionBuilder : SkillFactory
{
    public static List<string> SkillNames;
    public static List<string> LibrarySkillNames;

    public override void BuildSkill()
    {
        List<Skill> skills = new List<Skill>();
        SkillNames = new List<string>();
        LibrarySkillNames = new List<string>();

        // 续脉通络
        skills.Add(new EffectExcecuteSkillPro("续脉通络", Quality.Uncommon)
        {
            DamageRate = 0f, // 无伤害
            ManaCost = 2 * 0, // 无内力消耗
            CD = 3, // 冷却时间
            Preempt = true,
            Discription = "恢复自身8%血量和内力（受医术加成）",
            NoDamage = true,
            AttributeSelector = skillPro =>
            {
                skillPro.BaseExecute();
                int medicineLevel = skillPro.Loader.Player.SkillList.Medicine.getLevel(); // 获取医术等级
                float healthRestore = 0.08f + (medicineLevel * 0.005f); // 恢复血量百分比
                float manaRestore = 0.08f + (medicineLevel * 0.005f); // 恢复内力百分比

                int healthAmount = (int)(skillPro.Loader.Player.AttributePart.getMaxHealth() * healthRestore);
                int manaAmount = (int)(skillPro.Loader.Player.AttributePart.getMaxMana() * manaRestore);

                skillPro.Loader.RecoverHealth(healthAmount); // 恢复血量
                skillPro.Loader.RecoverMana(manaAmount); // 恢复内力
            }
        });

        // 蚀心咒
        skills.Add(new EffectExcecuteSkillPro("蚀心咒", Quality.Epic)
        {
            DamageRate = 0f, // 无伤害
            ManaCost = 2 * 1000, // 内力消耗
            CD = 3, // 冷却时间
            IsTimeLimit = true,
            Preempt = true,
            TimeUpperLimit = 2, // 可用次数2
            Discription = "扣除对方15%(受医术加成)的血量",
            NoDamage = true,
            AttributeSelector = skillPro =>
            {
                skillPro.BaseExecute();
                int medicineLevel = skillPro.Loader.Player.SkillList.Medicine.getLevel(); // 获取医术等级
                float healthLoss = 0.15f + (medicineLevel * 0.01f); // 扣除敌人血量的百分比

                int opponentHealthLoss = (int)(skillPro.Loader.GetOpponent().Player.AttributePart.getMaxHealth() * healthLoss);
                skillPro.Loader.GetOpponent().GetHurt(opponentHealthLoss); // 扣除敌人血量
            }
        });

        // 错经改穴
        skills.Add(new EffectExcecuteSkillPro("错经改穴", Quality.Rare)
        {
            DamageRate = 0f, // 无伤害
            ManaCost = 2 * 700, // 内力消耗
            CD = 3, // 冷却时间
            Preempt = true,
            Discription = "恢复自身10%(受医术加成)血量，并对敌人造成等量伤害",
            NoDamage = true,
            AttributeSelector = skillPro =>
            {
                skillPro.BaseExecute();
                int medicineLevel = skillPro.Loader.Player.SkillList.Medicine.getLevel(); // 获取医术等级
                float healthRestore = 0.1f + (medicineLevel * 0.01f); // 恢复血量百分比

                int healthAmount = (int)(skillPro.Loader.Player.AttributePart.getMaxHealth() * healthRestore);
                skillPro.Loader.RecoverHealth(healthAmount); // 恢复血量

                skillPro.Loader.GetOpponent().GetHurt(healthAmount); // 敌人受到等量伤害
            }
        });

        // 夺髓济生
        skills.Add(new EffectExcecuteSkillPro("夺髓济生", Quality.Legendary)
        {
            DamageRate = 0f, // 无伤害
            ManaCost = 2 * 600, // 内力消耗
            CD = 2, // 冷却时间
            Preempt = true,
            IsTimeLimit = true,
            TimeUpperLimit = 5, // 可用次数5
            Discription = "扣除对方8%(受医术加成)血量，并恢复等量血量（恢复上限2000）",
            NoDamage = true,
            AttributeSelector = skillPro =>
            {
                skillPro.BaseExecute();
                int medicineLevel = skillPro.Loader.Player.SkillList.Medicine.getLevel(); // 获取医术等级
                float healthLoss = 0.08f + (medicineLevel * 0.01f); // 扣除敌人血量的百分比

                int opponentHealthLoss = (int)(skillPro.Loader.GetOpponent().Player.AttributePart.getMaxHealth() * healthLoss);
                skillPro.Loader.GetOpponent().GetHurt(opponentHealthLoss); // 扣除敌人血量

                int recoveryAmount = Math.Min(opponentHealthLoss, 2000); // 恢复的血量不超过2000
                skillPro.Loader.RecoverHealth(recoveryAmount); // 恢复血量
            }
        });

        // 获取技能列表并加入到技能库
        foreach (Skill skill in skills)
        {
            SkillNames.Add(skill.Name);
        }
        SkillBuilder.SkillList.AddRange(skills);
    }
}

```



---

## TianGangGuYuanSkillFactory（降上限 · 抵御 · 护盾核爆）

### 用途与要点
- **以“最大生命上限”为成本**换取强势效果：抵御层数、超额护盾、群体上毒等（高风险高收益）。
- `GangYu/DiYu` 等为“抵御”效果：**免伤或抵消**一次/若干次伤害（按实现）。

### 关键点速读
- `御气法`：+1 层抵御并**提前 20%**（`Preempt/NoDamage`）。
- `玄罡诀`：**降低自身 5% 上限生命** → 获得 1 层抵御（限 2 次）。
- `铁御`：**降低 5% 上限** → 获得“扣除量×20”护盾（限 2 次）。
- `血湮`：**降低 5% 上限** → 敌方**7 层随机毒**（限 3 次）。

### 原始源码（保持不变）

```csharp
﻿using System;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]

public class TianGangGuYuanSkillFactory : SkillFactory
{
    public static List<string> skillNames;

    public override void BuildSkill()
    {
        List<Skill> SkillList = new List<Skill>();
        skillNames = new List<string>();

        // 御气法
        SkillList.Add(new EffectExcecuteSkillPro("御气法", Quality.Uncommon)
        {
            DamageRate = 0f, // 无伤害
            ManaCost = 2 * 0,
            CD = 2, // 冷却时间
            Discription = "获得一层【抵御】下次行动提前20%",
            NoDamage = true,
            IsTimeLimit = true, // 限制使用次数
            TimeUpperLimit = 2,
            AttributeSelector = Skill =>
            {

                Skill.Loader.AddEffect(new GangYu { Intensity = 1 });
                Skill.Loader.CD += 0.2f;
                Skill.BaseExecute();
            }
        });

        // 玄罡诀
        SkillList.Add(new EffectExcecuteSkillPro("玄罡诀", Quality.Uncommon)
        {
            DamageRate = 0f, // 无伤害
            ManaCost = 2 * 0,
            CD = 2, // 冷却时间
            Discription = "扣除自身5%血量上限 获得1层【抵御】",
            NoDamage = true,
            IsTimeLimit = true, // 限制使用次数
            TimeUpperLimit = 2,
            AttributeSelector = Skill =>
            {


                Skill.Loader.EnhanceMaxHealthByMultiply(-0.05f);

                Skill.Loader.AddEffect(new DiYu(1));

                Skill.BaseExecute();

            }
        });

        // 铁御
        SkillList.Add(new EffectExcecuteSkillPro("铁御", Quality.Rare)
        {
            DamageRate = 0f, // 无伤害
            ManaCost = 2 * 0,
            CD = 2, // 冷却时间
            Discription = "扣除自身5%血量上限 获得等同于扣除量X20的护盾",
            NoDamage = true,
            IsTimeLimit = true, // 限制使用次数
            TimeUpperLimit = 2,
            AttributeSelector = Skill =>
            {

                int lostHealth = (int)(Skill.Loader.Player.AttributePart.getMaxHealth() * 0.05f);
                int armorAmount = lostHealth * 20;
                Skill.Loader.EnhanceMaxHealthByMultiply(-0.05f);
                Skill.Loader.ModifyArmor(armorAmount);


                Skill.BaseExecute();

            }
        });

        // 血湮
        SkillList.Add(new EffectExcecuteSkillPro("血湮", Quality.Rare)
        {
            DamageRate = 0f, // 无伤害
            ManaCost = 2 * 0,
            CD = 3, // 冷却时间
            Discription = "扣除自身5%血量上限 为敌人附上7层随机的【毒】",
            NoDamage = true,
            IsTimeLimit = true, // 限制使用次数
            TimeUpperLimit = 3,
            AttributeSelector = Skill =>
            {

                int lostHealth = (int)(Skill.Loader.Player.AttributePart.getMaxHealth() * 0.05f);
                Skill.Loader.EnhanceMaxHealthByMultiply(-0.05f);
                for (int i = 0; i < 7; i++)
                {
                    PosionEffect poisonEffect = PosionFactory.RandomToxin();
                    Skill.Loader.GetOpponent().AddEffect(poisonEffect);
                }
                Skill.BaseExecute();

            }
        });

        foreach (Skill skill in SkillList)
        {
            skillNames.Add(skill.Name);
        }
        SkillBuilder.SkillList.AddRange(SkillList);
    }
}

```


---

## QingShenSkillFactory（轻身层数与闪避耦合）

### 用途与要点
- **“轻身”层数 = 节奏资源**：提供**提前行动**、**倍率加成**、**治疗/内力回复**等联动。
- 与“闪避”耦合：按基础闪避折算倍率与回复。

### 关键点速读
- `轻身术/飘渺步`：直接获得轻身层数，且**提前 50%**（`Preempt`）。
- `流风回雪`：按**当前闪避**回复血/内力。
- `飞燕还巢`：每有 1 层轻身，威力 +30（`Preempt`）。
- `浮光掠影`：每层轻身**提前 10%**，最多提前 70%。
- `凤舞九天`：每次使用按（闪避/2）提升**持久**招式威力（成长）。

### 原始源码（保持不变）

```csharp
﻿using System;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class QingShenSkillFactory : SkillFactory
{
    public static List<string> skillNames;

    public override void BuildSkill()
    {
        List<Skill> SkillList = new List<Skill>();
        skillNames = new List<string>();

        // 轻身术
        SkillList.Add(new EffectExcecuteSkillPro("轻身术", Quality.Uncommon)
        {
            DamageRate = 0f,
            ManaCost = 2 * 100,
            CD = 2,
            Discription = "获得2层【轻身】",
            NoDamage = true,
            Preempt = true, // 先制
            AttributeSelector = Skill =>
            {
                Skill.BaseExecute();
                Skill.Loader.AddEffect(new QingShenEffect(2));
            }
        });

        // 飘渺步
        SkillList.Add(new EffectExcecuteSkillPro("飘渺步", Quality.Uncommon)
        {
            DamageRate = 0f,
            ManaCost = 2 * 150,
            CD = 2,
            Discription = "获得1层【轻身】，下次行动提前50%",
            NoDamage = true,
            Preempt = true, // 先制
            AttributeSelector = Skill =>
            {
                Skill.BaseExecute();
                Skill.Loader.AddEffect(new QingShenEffect(1));
                Skill.Loader.CD += 0.5f;
            }
        });


        // 流风回雪
        SkillList.Add(new EffectExcecuteSkillPro("流风回雪", Quality.Rare)
        {
            DamageRate = 0f,
            ManaCost = 2 * 0,
            CD = 3,
            Discription = "获得2层【轻身】恢复等同于当前闪避值的血量和内力",
            NoDamage = true,
            AttributeSelector = Skill =>
            {
                Skill.BaseExecute();
                Skill.Loader.AddEffect(new QingShenEffect(2));
                int recoveryAmount = (int)AttributeCalculator.GetBaseEvasion(Skill.Loader.Player);
                Skill.Loader.RecoverHealth(recoveryAmount);
                Skill.Loader.RecoverMana(recoveryAmount);
            }
        });

        // 飞燕还巢
        SkillList.Add(new EffectExcecuteSkillPro("飞燕还巢", Quality.Rare)
        {
            DamageRate = 1.5f,
            ManaCost = 2 * 200,
            CD = 4,
            Discription = "自身每有一层【轻身】，威力+30",
            Preempt = true, // 先制
            AttributeSelector = Skill =>
            {
                int qingShenLayers = Skill.Loader.GetEffectIntensityByName("轻身");
                Skill.DamageRate += qingShenLayers * 0.3f;
                Skill.BaseExecute();
                Skill.DamageRate -= qingShenLayers * 0.3f;
            }
        });

        // 浮光掠影
        SkillList.Add(new EffectExcecuteSkillPro("浮光掠影", Quality.Epic)
        {
            DamageRate = 1.2f,
            ManaCost = 2 * 350,
            CD = 2,
            Discription = "自身每有1层【轻身】，下次行动提前10%（最多提前70%）",
            AttributeSelector = Skill =>
            {
                int qingShenLayers = Skill.Loader.GetEffectIntensityByName("轻身");
                float speedBoost = Math.Min(qingShenLayers * 0.1f, 0.70f);
                Skill.Loader.CD += speedBoost;
                Skill.BaseExecute();
            }
        });

        // 凤舞九天
        SkillList.Add(new EffectExcecuteSkillPro("凤舞九天", Quality.Epic)
        {
            DamageRate = 2.5f,
            ManaCost = 2 * 1000,
            CD = 4,
            AppearWeight = 1,
            Discription = "每次使用后，增加等同于自身闪避值/2的招式威力",
            AttributeSelector = Skill =>
            {
                float evasionBonus = (int)AttributeCalculator.GetBaseEvasion(Skill.Loader.Player) / 2;
                Skill.DamageRate += evasionBonus / 100;
                Skill.BaseExecute();
            }
        });

        foreach (Skill skill in SkillList)
        {
            skillNames.Add(skill.Name);
            skill.AppearWeight += 1;
        }
        SkillBuilder.SkillList.AddRange(SkillList);
    }
}

```


---
