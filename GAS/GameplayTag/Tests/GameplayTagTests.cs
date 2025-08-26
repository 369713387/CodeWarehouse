using UnityEngine;
using CodeWarehouse.GAS;

namespace CodeWarehouse.GAS.Tests
{
    /// <summary>
    /// GameplayTag 系统测试类
    /// 包含各种单元测试用例来验证系统功能的正确性
    /// </summary>
    public class GameplayTagTests : MonoBehaviour
    {
        [Header("测试控制")]
        [SerializeField] private bool runTestsOnStart = true;
        [SerializeField] private bool logDetailedResults = true;

        private int _totalTests = 0;
        private int _passedTests = 0;
        private int _failedTests = 0;

        private void Start()
        {
            if (runTestsOnStart)
            {
                RunAllTests();
            }
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        [ContextMenu("Run All Tests")]
        public void RunAllTests()
        {
            Debug.Log("=== 开始运行 GameplayTag 系统测试 ===");
            _totalTests = 0;
            _passedTests = 0;
            _failedTests = 0;

            // 基础功能测试
            TestBasicTagCreation();
            TestTagComparison();
            TestTagProperties();
            TestTagHierarchy();

            // 容器测试
            TestContainerBasicOperations();
            TestContainerQueries();
            TestContainerSetOperations();

            // 管理器测试
            TestTagManagerRegistration();
            TestTagManagerValidation();

            // 性能测试
            TestPerformance();

            // 输出测试结果
            Debug.Log($"\n=== 测试完成 ===");
            Debug.Log($"总测试数: {_totalTests}");
            Debug.Log($"通过: {_passedTests}");
            Debug.Log($"失败: {_failedTests}");
            Debug.Log($"成功率: {(_passedTests * 100.0f / _totalTests):F1}%");

            if (_failedTests == 0)
            {
                Debug.Log("<color=green>所有测试通过！</color>");
            }
            else
            {
                Debug.LogError($"<color=red>有 {_failedTests} 个测试失败！</color>");
            }
        }

        #region 基础功能测试

        private void TestBasicTagCreation()
        {
            LogTestGroup("基础标签创建测试");

            // 测试正常创建
            AssertTrue("创建有效标签", () =>
            {
                var tag = new GameplayTag("Character.State.Idle");
                return !tag.IsEmpty && tag.Name == "Character.State.Idle";
            });

            // 测试空标签
            AssertTrue("创建空标签", () =>
            {
                var tag = new GameplayTag("");
                return tag.IsEmpty && tag.Name == "";
            });

            // 测试null标签
            AssertTrue("创建null标签", () =>
            {
                var tag = new GameplayTag(null);
                return tag.IsEmpty;
            });

            // 测试隐式转换
            AssertTrue("字符串隐式转换", () =>
            {
                GameplayTag tag = "Test.Tag";
                return tag.Name == "Test.Tag";
            });
        }

        private void TestTagComparison()
        {
            LogTestGroup("标签比较测试");

            var tag1 = new GameplayTag("Character.State.Idle");
            var tag2 = new GameplayTag("Character.State.Idle");
            var tag3 = new GameplayTag("Character.State.Moving");

            // 测试相等比较
            AssertTrue("相同标签相等", () => tag1 == tag2);
            AssertTrue("Equals方法", () => tag1.Equals(tag2));
            AssertTrue("不同标签不相等", () => tag1 != tag3);

            // 测试哈希码
            AssertTrue("相同标签哈希码相等", () => tag1.GetHashCode() == tag2.GetHashCode());
        }

        private void TestTagProperties()
        {
            LogTestGroup("标签属性测试");

            var tag = new GameplayTag("Character.State.Combat.Attacking");

            AssertEquals("完整名称", tag.Name, "Character.State.Combat.Attacking");
            AssertEquals("简称", tag.ShortName, "Attacking");
            AssertEquals("深度", tag.Depth, 4);
            AssertEquals("祖先数量", tag.AncestorNames.Length, 3);
            AssertEquals("第一个祖先", tag.AncestorNames[0], "Character");
            AssertEquals("第二个祖先", tag.AncestorNames[1], "Character.State");
            AssertEquals("第三个祖先", tag.AncestorNames[2], "Character.State.Combat");
        }

        private void TestTagHierarchy()
        {
            LogTestGroup("标签层级测试");

            var fullTag = new GameplayTag("Character.State.Combat.Attacking");
            var parentTag = new GameplayTag("Character.State.Combat");
            var rootTag = new GameplayTag("Character");
            var unrelatedTag = new GameplayTag("Skill.Magic.Fire");

            // 测试HasTag
            AssertTrue("拥有父标签", () => fullTag.HasTag(parentTag));
            AssertTrue("拥有根标签", () => fullTag.HasTag(rootTag));
            AssertTrue("拥有自身", () => fullTag.HasTag(fullTag));
            AssertFalse("不拥有无关标签", () => fullTag.HasTag(unrelatedTag));

            // 测试IsDescendantOf
            AssertTrue("是父标签的子标签", () => fullTag.IsDescendantOf(parentTag));
            AssertTrue("是根标签的子标签", () => fullTag.IsDescendantOf(rootTag));

            // 测试GetParent
            AssertEquals("获取父标签", fullTag.GetParent().Name, "Character.State.Combat");

            // 测试GetAncestorAtLevel
            AssertEquals("获取层级0祖先", fullTag.GetAncestorAtLevel(0).Name, "Character");
            AssertEquals("获取层级1祖先", fullTag.GetAncestorAtLevel(1).Name, "Character.State");
        }

        #endregion

        #region 容器测试

        private void TestContainerBasicOperations()
        {
            LogTestGroup("容器基础操作测试");

            var container = new GameplayTagContainer();

            // 测试添加
            AssertTrue("添加标签", () => container.AddTag("Test.Tag1"));
            AssertFalse("重复添加标签", () => container.AddTag("Test.Tag1"));
            AssertEquals("容器大小", container.Count, 1);

            // 测试移除
            AssertTrue("移除标签", () => container.RemoveTag("Test.Tag1"));
            AssertFalse("移除不存在的标签", () => container.RemoveTag("Test.Tag1"));
            AssertEquals("移除后容器大小", container.Count, 0);

            // 测试批量添加
            container.AddTags("Tag1", "Tag2", "Tag3");
            AssertEquals("批量添加后大小", container.Count, 3);

            // 测试清空
            container.Clear();
            AssertTrue("清空后为空", () => container.IsEmpty);
        }

        private void TestContainerQueries()
        {
            LogTestGroup("容器查询测试");

            var container = new GameplayTagContainer(
                "Character.State.Combat.Attacking",
                "Character.State.Combat.Defending",
                "Skill.Magic.Fire",
                "Skill.Magic.Ice"
            );

            // 测试精确匹配
            AssertTrue("包含精确标签", () => container.HasTag("Character.State.Combat.Attacking"));
            AssertFalse("不包含不存在的标签", () => container.HasTag("Character.State.Running"));

            // 测试层级匹配
            AssertTrue("包含层级标签", () => container.HasTagExact("Character.State.Combat"));
            AssertTrue("包含根标签", () => container.HasTagExact("Character"));
            AssertTrue("包含魔法标签", () => container.HasTagExact("Skill.Magic"));

            // 测试批量查询
            AssertTrue("包含任意标签", () => container.HasAnyTag(
                new GameplayTag("Character.State.Combat.Attacking"),
                new GameplayTag("NonExistent.Tag")
            ));

            AssertFalse("包含所有标签（包含不存在的）", () => container.HasAllTags(
                new GameplayTag("Character.State.Combat.Attacking"),
                new GameplayTag("NonExistent.Tag")
            ));
        }

        private void TestContainerSetOperations()
        {
            LogTestGroup("容器集合操作测试");

            var container1 = new GameplayTagContainer("Tag1", "Tag2", "Tag3");
            var container2 = new GameplayTagContainer("Tag3", "Tag4", "Tag5");

            // 测试合并
            var unionResult = container1 + container2;
            AssertEquals("合并后大小", unionResult.Count, 5);

            // 测试差集
            var diffResult = container1 - container2;
            AssertEquals("差集大小", diffResult.Count, 2);
            AssertTrue("差集包含Tag1", () => diffResult.HasTag("Tag1"));
            AssertTrue("差集包含Tag2", () => diffResult.HasTag("Tag2"));

            // 测试交集
            var container3 = new GameplayTagContainer(container1);
            container3.Intersect(container2);
            AssertEquals("交集大小", container3.Count, 1);
            AssertTrue("交集包含Tag3", () => container3.HasTag("Tag3"));
        }

        #endregion

        #region 管理器测试

        private void TestTagManagerRegistration()
        {
            LogTestGroup("标签管理器注册测试");

            var manager = GameplayTagManager.Instance;

            // 注册标签
            var tag = manager.RegisterTag("Test.Manager.Tag");
            AssertFalse("注册的标签不为空", () => tag.IsEmpty);
            AssertTrue("标签已注册", () => manager.IsTagRegistered("Test.Manager.Tag"));

            // 重复注册
            var tag2 = manager.RegisterTag("Test.Manager.Tag");
            AssertTrue("重复注册返回相同标签", () => tag == tag2);

            // 获取标签
            var retrievedTag = manager.GetTag("Test.Manager.Tag");
            AssertTrue("获取到相同标签", () => tag == retrievedTag);

            // 注销标签
            bool unregistered = manager.UnregisterTag("Test.Manager.Tag");
            AssertTrue("成功注销标签", () => unregistered);
            AssertFalse("标签已注销", () => manager.IsTagRegistered("Test.Manager.Tag"));
        }

        private void TestTagManagerValidation()
        {
            LogTestGroup("标签管理器验证测试");

            var manager = GameplayTagManager.Instance;

            // 测试有效标签名
            var validResult = manager.ValidateTagName("Valid.Tag.Name");
            AssertTrue("有效标签名验证通过", () => validResult.IsValid);

            // 测试无效标签名
            var invalidResult1 = manager.ValidateTagName("");
            AssertFalse("空标签名验证失败", () => invalidResult1.IsValid);

            var invalidResult2 = manager.ValidateTagName("Invalid..Tag");
            AssertFalse("连续点号验证失败", () => invalidResult2.IsValid);

            var invalidResult3 = manager.ValidateTagName(".InvalidStart");
            AssertFalse("点号开头验证失败", () => invalidResult3.IsValid);

            var invalidResult4 = manager.ValidateTagName("InvalidEnd.");
            AssertFalse("点号结尾验证失败", () => invalidResult4.IsValid);
        }

        #endregion

        #region 性能测试

        private void TestPerformance()
        {
            LogTestGroup("性能测试");

            const int testCount = 10000;

            // 创建测试标签
            var tags = new GameplayTag[testCount];
            var startTime = Time.realtimeSinceStartup;

            for (int i = 0; i < testCount; i++)
            {
                tags[i] = new GameplayTag($"Performance.Test.Tag{i}");
            }

            var creationTime = Time.realtimeSinceStartup - startTime;
            LogTestResult($"创建{testCount}个标签耗时", $"{creationTime * 1000:F2}ms");

            // 比较性能测试
            startTime = Time.realtimeSinceStartup;
            int comparisons = 0;

            for (int i = 0; i < testCount; i++)
            {
                for (int j = i + 1; j < testCount && j < i + 100; j++) // 限制比较次数避免过长
                {
                    bool equal = tags[i] == tags[j];
                    comparisons++;
                }
            }

            var comparisonTime = Time.realtimeSinceStartup - startTime;
            LogTestResult($"执行{comparisons}次比较耗时", $"{comparisonTime * 1000:F2}ms");

            // 层级查询性能测试
            var hierarchicalTag = new GameplayTag("Performance.Test.Deep.Hierarchy.Tag");
            var queryTag = new GameplayTag("Performance.Test");

            startTime = Time.realtimeSinceStartup;
            for (int i = 0; i < testCount; i++)
            {
                bool hasTag = hierarchicalTag.HasTag(queryTag);
            }
            var queryTime = Time.realtimeSinceStartup - startTime;
            LogTestResult($"执行{testCount}次层级查询耗时", $"{queryTime * 1000:F2}ms");
        }

        #endregion

        #region 测试辅助方法

        private void LogTestGroup(string groupName)
        {
            if (logDetailedResults)
            {
                Debug.Log($"\n--- {groupName} ---");
            }
        }

        private void AssertTrue(string testName, System.Func<bool> condition)
        {
            _totalTests++;
            try
            {
                bool result = condition();
                if (result)
                {
                    _passedTests++;
                    LogTestResult(testName, "通过");
                }
                else
                {
                    _failedTests++;
                    LogTestResult(testName, "失败 - 条件为false");
                }
            }
            catch (System.Exception e)
            {
                _failedTests++;
                LogTestResult(testName, $"失败 - 异常: {e.Message}");
            }
        }

        private void AssertFalse(string testName, System.Func<bool> condition)
        {
            AssertTrue(testName, () => !condition());
        }

        private void AssertEquals<T>(string testName, T actual, T expected)
        {
            AssertTrue(testName, () => 
            {
                if (actual == null && expected == null) return true;
                if (actual == null || expected == null) return false;
                return actual.Equals(expected);
            });
        }

        private void LogTestResult(string testName, string result)
        {
            if (logDetailedResults)
            {
                string color = result.Contains("通过") ? "green" : "red";
                Debug.Log($"<color={color}>{testName}: {result}</color>");
            }
        }

        #endregion
    }
}
