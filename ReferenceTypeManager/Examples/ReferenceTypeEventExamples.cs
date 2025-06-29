using UnityEngine;
using Framework.ZeroGCEventBus;
using System.Collections.Generic;

namespace Framework.ZeroGCEventBus.Examples
{
    #region 示例数据类

    /// <summary>
    /// 示例玩家数据类
    /// </summary>
    public class PlayerData
    {
        public string PlayerName;
        public int Level;
        public float Health;
        public float MaxHealth;
        public Vector3 Position;
        
        public PlayerData(string name, int level)
        {
            PlayerName = name;
            Level = level;
            MaxHealth = 100f + level * 10f;
            Health = MaxHealth;
        }
    }

    /// <summary>
    /// 示例武器类
    /// </summary>
    [CreateAssetMenu(fileName = "New Weapon", menuName = "Example/Weapon")]
    public class Weapon : ScriptableObject
    {
        public string WeaponName;
        public float Damage;
        public float AttackSpeed;
        public string[] Effects;
    }

    /// <summary>
    /// 示例物品类
    /// </summary>
    public class Item
    {
        public int ItemId;
        public string ItemName;
        public int Quantity;
        public string Description;
        
        public Item(int id, string name, int quantity = 1)
        {
            ItemId = id;
            ItemName = name;
            Quantity = quantity;
        }
    }

    /// <summary>
    /// 示例场景数据类
    /// </summary>
    public class SceneData
    {
        public string SceneName;
        public List<GameObject> StaticObjects;
        public Dictionary<string, Material> Materials;
        public AudioClip BackgroundMusic;

        public SceneData(string name)
        {
            SceneName = name;
            StaticObjects = new List<GameObject>();
            Materials = new Dictionary<string, Material>();
        }
    }

    #endregion

    #region 游戏事件定义

    /// <summary>
    /// 玩家攻击事件 - 使用新的扩展方法
    /// </summary>
    public struct PlayerAttackEvent : ZeroGCEventBus.IZeroGCEvent
    {
        public int AttackerId;        // 攻击者GameObject的ID
        public int TargetId;          // 目标GameObject的ID  
        public int WeaponId;          // 武器ScriptableObject的ID
        public int AttackerTransformId; // 攻击者Transform的ID
        public Vector3 AttackPosition;
        public float Damage;
        public float CriticalChance;
        
        // 便利方法 - 使用新的扩展方法
        public GameObject GetAttacker() => ReferenceTypeManager.GetReference<GameObject>(AttackerId);
        public GameObject GetTarget() => ReferenceTypeManager.GetReference<GameObject>(TargetId);
        public Weapon GetWeapon() => ReferenceTypeExtensions.GetScriptableObject<Weapon>(WeaponId);
        public Transform GetAttackerTransform() => ReferenceTypeExtensions.GetTransform(AttackerTransformId);
        
        // 静态创建方法 - 使用新的扩展方法
        public static PlayerAttackEvent Create(GameObject attacker, GameObject target, Weapon weapon, 
                                             Vector3 position, float damage, float critChance = 0.1f)
        {
            return new PlayerAttackEvent
            {
                AttackerId = attacker.ToReferenceId(),
                TargetId = target.ToReferenceId(),
                WeaponId = weapon.ToScriptableObjectId(),
                AttackerTransformId = attacker.transform.ToReferenceId(),
                AttackPosition = position,
                Damage = damage,
                CriticalChance = critChance
            };
        }
    }

    /// <summary>
    /// 物理碰撞事件 - 展示物理组件扩展的使用
    /// </summary>
    public struct PhysicsCollisionEvent : ZeroGCEventBus.IZeroGCEvent
    {
        private int ColliderId;       // 碰撞器ID
        private int RigidbodyId;       // 刚体ID
        public Vector3 CollisionPoint;
        public Vector3 CollisionNormal;
        public float CollisionForce;
        public CollisionType Type;

        public enum CollisionType
        {
            Enter,
            Stay,
            Exit
        }

        // 便利方法 - 使用物理组件扩展
        public Collider GetCollider() => ReferenceTypeExtensions.GetPhysicsComponent<Collider>(ColliderId);
        public Rigidbody GetRigidbody() => ReferenceTypeExtensions.GetPhysicsComponent<Rigidbody>(RigidbodyId);

        public static PhysicsCollisionEvent Create(Collision collision, CollisionType type)
        {
            return new PhysicsCollisionEvent
            {
                ColliderId = collision.collider.ToPhysicsReferenceId(),
                RigidbodyId = collision.rigidbody?.ToPhysicsReferenceId() ?? 0,
                CollisionPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : Vector3.zero,
                CollisionNormal = collision.contacts.Length > 0 ? collision.contacts[0].normal : Vector3.up,
                CollisionForce = collision.impulse.magnitude,
                Type = type
            };
        }
    }

    /// <summary>
    /// UI交互事件 - 展示UI组件扩展的使用
    /// </summary>
    public struct UIInteractionEvent : ZeroGCEventBus.IZeroGCEvent
    {
        public int UIElementId;       // UI元素GameObject的ID
        public int CanvasId;          // Canvas的ID
        public int RectTransformId;   // RectTransform的ID
        public int CanvasGroupId;     // CanvasGroup的ID
        public UIInteractionType InteractionType;
        public Vector2 ScreenPosition;
        public string InteractionData;

        public enum UIInteractionType
        {
            Click,
            Hover,
            Drag,
            Drop,
            Focus,
            Blur
        }

        // 便利方法 - 使用UI组件扩展
        public GameObject GetUIElement() => ReferenceTypeManager.GetReference<GameObject>(UIElementId);
        public Canvas GetCanvas() => ReferenceTypeExtensions.GetUIComponent<Canvas>(CanvasId);
        public RectTransform GetRectTransform() => ReferenceTypeExtensions.GetUIComponent<RectTransform>(RectTransformId);
        public CanvasGroup GetCanvasGroup() => ReferenceTypeExtensions.GetUIComponent<CanvasGroup>(CanvasGroupId);

        public static UIInteractionEvent Create(GameObject uiElement, UIInteractionType type, 
                                              Vector2 screenPos, string data = "")
        {
            var rectTransform = uiElement.GetComponent<RectTransform>();
            var canvas = uiElement.GetComponentInParent<Canvas>();
            var canvasGroup = uiElement.GetComponent<CanvasGroup>();

            return new UIInteractionEvent
            {
                UIElementId = uiElement.ToReferenceId(),
                CanvasId = canvas?.ToUIReferenceId() ?? 0,
                RectTransformId = rectTransform?.ToUIReferenceId() ?? 0,
                CanvasGroupId = canvasGroup?.ToUIReferenceId() ?? 0,
                InteractionType = type,
                ScreenPosition = screenPos,
                InteractionData = data
            };
        }
    }

    /// <summary>
    /// 资源加载事件 - 展示资源类型扩展的使用
    /// </summary>
    public struct AssetLoadEvent : ZeroGCEventBus.IZeroGCEvent
    {
        public int MaterialId;        // Material资源ID
        public int TextureId;         // Texture资源ID
        public int SpriteId;          // Sprite资源ID
        public int MeshId;            // Mesh资源ID
        public int AudioClipId;       // AudioClip资源ID
        public int AnimationClipId;   // AnimationClip资源ID
        public int ShaderId;          // Shader资源ID
        public int FontId;            // Font资源ID
        public AssetLoadType LoadType;
        public float LoadProgress;
        public bool IsComplete;

        public enum AssetLoadType
        {
            Preload,
            OnDemand,
            Background,
            Unload
        }

        // 便利方法 - 使用资源扩展方法
        public Material GetMaterial() => ReferenceTypeExtensions.GetAsset<Material>(MaterialId);
        public Texture GetTexture() => ReferenceTypeExtensions.GetAsset<Texture>(TextureId);
        public Sprite GetSprite() => ReferenceTypeExtensions.GetAsset<Sprite>(SpriteId);
        public Mesh GetMesh() => ReferenceTypeExtensions.GetAsset<Mesh>(MeshId);
        public AudioClip GetAudioClip() => ReferenceTypeExtensions.GetAsset<AudioClip>(AudioClipId);
        public AnimationClip GetAnimationClip() => ReferenceTypeExtensions.GetAsset<AnimationClip>(AnimationClipId);
        public Shader GetShader() => ReferenceTypeExtensions.GetAsset<Shader>(ShaderId);
        public Font GetFont() => ReferenceTypeExtensions.GetAsset<Font>(FontId);

        public static AssetLoadEvent Create(Material material, Texture texture, Sprite sprite, 
                                          AssetLoadType type, float progress = 1f)
        {
            return new AssetLoadEvent
            {
                MaterialId = material?.ToAssetReferenceId() ?? 0,
                TextureId = texture?.ToAssetReferenceId() ?? 0,
                SpriteId = sprite?.ToAssetReferenceId() ?? 0,
                MeshId = 0,
                AudioClipId = 0,
                AnimationClipId = 0,
                ShaderId = 0,
                FontId = 0,
                LoadType = type,
                LoadProgress = progress,
                IsComplete = progress >= 1f
            };
        }
    }

    /// <summary>
    /// 物品交互事件 - 使用ReferenceIdContainer
    /// </summary>
    public struct ItemInteractionEvent : ZeroGCEventBus.IZeroGCEvent
    {
        public ReferenceIdContainer References; // Player, Item, Container
        public Vector3 InteractionPosition;
        public InteractionType Type;
        public int Quantity;
        
        public enum InteractionType
        {
            PickUp,
            Drop,
            Use,
            Combine
        }
        
        // 便利方法
        public GameObject GetPlayer() => References.GetPrimary<GameObject>();
        public object GetItem() => References.GetSecondary<object>();
        public GameObject GetContainer() => References.GetTertiary<GameObject>();
        
        public static ItemInteractionEvent Create(GameObject player, object item, InteractionType type, 
                                                int quantity = 1, GameObject container = null, Vector3? position = null)
        {
            return new ItemInteractionEvent
            {
                References = new ReferenceIdContainer(
                    player.ToReferenceId(),
                    ReferenceTypeExtensions.GetReferenceId(item),
                    container?.ToReferenceId() ?? 0
                ),
                InteractionPosition = position ?? Vector3.zero,
                Type = type,
                Quantity = quantity
            };
        }
    }

    /// <summary>
    /// 多目标技能事件 - 使用ReferenceIdArray
    /// </summary>
    public struct MultiTargetSkillEvent : ZeroGCEventBus.IZeroGCEvent
    {
        public int CasterId;              // 施法者ID
        public int SkillDataId;           // 技能数据ID
        public ReferenceIdArray TargetIds; // 目标ID数组
        public Vector3 CastPosition;
        public float SkillPower;
        public bool IsCritical;
        
        // 便利方法
        public GameObject GetCaster() => ReferenceTypeManager.GetReference<GameObject>(CasterId);
        public ScriptableObject GetSkillData() => ReferenceTypeManager.GetReference<ScriptableObject>(SkillDataId);
        public GameObject[] GetTargets() => TargetIds.GetReferences<GameObject>();
        public List<GameObject> GetValidTargets() => TargetIds.GetReferencesList<GameObject>();
        
        public static MultiTargetSkillEvent Create(GameObject caster, ScriptableObject skillData, 
                                                 GameObject[] targets, Vector3 position, float power, bool critical = false)
        {
            return new MultiTargetSkillEvent
            {
                CasterId = caster.ToReferenceId(),
                SkillDataId = skillData.ToScriptableObjectId(),
                TargetIds = new ReferenceIdArray(targets.ToReferenceIds()),
                CastPosition = position,
                SkillPower = power,
                IsCritical = critical
            };
        }
    }

    /// <summary>
    /// 场景数据事件 - 展示ReferenceIdDictionary的使用
    /// </summary>
    public struct SceneDataEvent : ZeroGCEventBus.IZeroGCEvent
    {
        public ReferenceIdDictionary AssetReferences; // 场景资源字典
        public HierarchyReferenceContainer SceneHierarchy; // 场景层级结构
        public string SceneName;
        public SceneEventType EventType;

        public enum SceneEventType
        {
            Load,
            Unload,
            AssetUpdate,
            HierarchyChange
        }

        // 便利方法
        public Material GetMaterial(string key) => AssetReferences.GetReference<Material>(key);
        public AudioClip GetAudioClip(string key) => AssetReferences.GetReference<AudioClip>(key);
        public GameObject GetSceneRoot() => SceneHierarchy.GetParent<GameObject>();
        public List<GameObject> GetSceneObjects() => SceneHierarchy.GetValidChildren<GameObject>();

        public static SceneDataEvent Create(SceneData sceneData, GameObject sceneRoot, 
                                          GameObject[] sceneObjects, SceneEventType eventType)
        {
            // 创建资源字典
            var assetDict = new Dictionary<string, object>();
            if (sceneData.Materials != null)
            {
                foreach (var kvp in sceneData.Materials)
                {
                    assetDict[$"material_{kvp.Key}"] = kvp.Value;
                }
            }
            if (sceneData.BackgroundMusic != null)
            {
                assetDict["background_music"] = sceneData.BackgroundMusic;
            }

            return new SceneDataEvent
            {
                AssetReferences = new ReferenceIdDictionary(assetDict),
                SceneHierarchy = new HierarchyReferenceContainer(sceneRoot, sceneObjects),
                SceneName = sceneData.SceneName,
                EventType = eventType
            };
        }
    }

    /// <summary>
    /// UI更新事件 - 保持原有功能
    /// </summary>
    public struct UIUpdateEvent : ZeroGCEventBus.IZeroGCEvent
    {
        public int UIElementId;       // UI元素的ID
        public int DataSourceId;      // 数据源的ID
        public UIUpdateType UpdateType;
        public string TextContent;
        public float NumericValue;
        public bool BooleanValue;
        
        public enum UIUpdateType
        {
            Text,
            Numeric,
            Boolean,
            Color,
            Visibility
        }
        
        // 便利方法
        public GameObject GetUIElement() => ReferenceTypeManager.GetReference<GameObject>(UIElementId);
        public object GetDataSource() => ReferenceTypeManager.GetReference<object>(DataSourceId);
        
        public static UIUpdateEvent CreateTextUpdate(GameObject uiElement, object dataSource, string text)
        {
            return new UIUpdateEvent
            {
                UIElementId = uiElement.ToReferenceId(),
                DataSourceId = ReferenceTypeExtensions.GetReferenceId(dataSource),
                UpdateType = UIUpdateType.Text,
                TextContent = text
            };
        }
        
        public static UIUpdateEvent CreateNumericUpdate(GameObject uiElement, object dataSource, float value)
        {
            return new UIUpdateEvent
            {
                UIElementId = uiElement.ToReferenceId(),
                DataSourceId = ReferenceTypeExtensions.GetReferenceId(dataSource),
                UpdateType = UIUpdateType.Numeric,
                NumericValue = value
            };
        }
    }

    #endregion

    #region 使用示例组件

    /// <summary>
    /// 示例玩家控制器 - 更新为使用新扩展方法
    /// </summary>
    public class ExamplePlayerController : MonoBehaviour
    {
        [SerializeField] private Weapon currentWeapon;
        [SerializeField] private float attackRange = 2f;
        
        private ZeroGCEventBus.EventHandlerId<PlayerAttackEvent> _attackEventHandler;
        private ZeroGCEventBus.EventHandlerId<ItemInteractionEvent> _itemEventHandler;
        private ZeroGCEventBus.EventHandlerId<PhysicsCollisionEvent> _collisionEventHandler;

        private void Start()
        {
            // 订阅事件
            _attackEventHandler = ZeroGCEventBus.Instance.Subscribe<PlayerAttackEvent>(OnPlayerAttack);
            _itemEventHandler = ZeroGCEventBus.Instance.Subscribe<ItemInteractionEvent>(OnItemInteraction);
            _collisionEventHandler = ZeroGCEventBus.Instance.Subscribe<PhysicsCollisionEvent>(OnPhysicsCollision);
        }

        private void Update()
        {
            // 示例：按空格键攻击
            if (Input.GetKeyDown(KeyCode.Space))
            {
                PerformAttack();
            }
        }

        private void PerformAttack()
        {
            // 寻找附近的目标
            var colliders = Physics.OverlapSphere(transform.position, attackRange);
            foreach (var collider in colliders)
            {
                if (collider.CompareTag("Enemy"))
                {
                    // 创建并发布攻击事件 - 使用新的扩展方法
                    var attackEvent = PlayerAttackEvent.Create(
                        gameObject,
                        collider.gameObject,
                        currentWeapon,
                        transform.position,
                        10f
                    );
                    
                    ZeroGCEventBus.Instance.Publish(attackEvent);
                    break;
                }
            }
        }

        // 碰撞检测
        private void OnCollisionEnter(Collision collision)
        {
            var collisionEvent = PhysicsCollisionEvent.Create(collision, PhysicsCollisionEvent.CollisionType.Enter);
            ZeroGCEventBus.Instance.Publish(collisionEvent);
        }

        // 事件处理方法 - 使用新的扩展方法
        private void OnPlayerAttack(PlayerAttackEvent eventData)
        {
            var attacker = eventData.GetAttacker();
            var target = eventData.GetTarget();
            var weapon = eventData.GetWeapon();
            var attackerTransform = eventData.GetAttackerTransform();
            
            if (attacker != null && target != null)
            {
                string weaponName = weapon != null ? weapon.WeaponName : "徒手";
                Debug.Log($"{attacker.name} 在位置 {attackerTransform?.position} 使用 {weaponName} 攻击了 {target.name}，造成 {eventData.Damage} 点伤害");
            }
        }

        private void OnItemInteraction(ItemInteractionEvent eventData)
        {
            var player = eventData.GetPlayer();
            var item = eventData.GetItem();
            
            if (player != null && item != null)
            {
                Debug.Log($"{player.name} {eventData.Type} 了物品");
            }
        }

        private void OnPhysicsCollision(PhysicsCollisionEvent eventData)
        {
            var collider = eventData.GetCollider();
            
            if (collider != null)
            {
                Debug.Log($"物体碰撞: {collider.name}，碰撞力度: {eventData.CollisionForce}");
            }
        }

        private void OnDestroy()
        {
            // 取消订阅
            if (_attackEventHandler.IsValid)
                ZeroGCEventBus.Instance.Unsubscribe(_attackEventHandler);
                
            if (_itemEventHandler.IsValid)
                ZeroGCEventBus.Instance.Unsubscribe(_itemEventHandler);

            if (_collisionEventHandler.IsValid)
                ZeroGCEventBus.Instance.Unsubscribe(_collisionEventHandler);
        }
    }

    /// <summary>
    /// UI交互管理器 - 展示UI扩展方法的使用
    /// </summary>
    public class UIInteractionManager : MonoBehaviour
    {
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private CanvasGroup dialogCanvasGroup;
        
        private ZeroGCEventBus.EventHandlerId<UIInteractionEvent> _uiInteractionHandler;
        private ZeroGCEventBus.EventHandlerId<UIUpdateEvent> _uiUpdateHandler;

        private void Start()
        {
            _uiInteractionHandler = ZeroGCEventBus.Instance.Subscribe<UIInteractionEvent>(OnUIInteraction);
            _uiUpdateHandler = ZeroGCEventBus.Instance.Subscribe<UIUpdateEvent>(OnUIUpdate);
        }

        public void HandleButtonClick(GameObject button)
        {
            // 使用新的UI扩展方法创建事件
            var uiEvent = UIInteractionEvent.Create(
                button, 
                UIInteractionEvent.UIInteractionType.Click,
                Input.mousePosition,
                "button_click"
            );
            
            ZeroGCEventBus.Instance.Publish(uiEvent);
        }

        private void OnUIInteraction(UIInteractionEvent eventData)
        {
            var uiElement = eventData.GetUIElement();
            var canvas = eventData.GetCanvas();
            var rectTransform = eventData.GetRectTransform();
            var canvasGroup = eventData.GetCanvasGroup();

            if (uiElement != null)
            {
                Debug.Log($"UI交互: {uiElement.name} 类型: {eventData.InteractionType}");
                
                if (canvas != null)
                    Debug.Log($"  所属Canvas: {canvas.name}");
                    
                if (rectTransform != null)
                    Debug.Log($"  RectTransform位置: {rectTransform.anchoredPosition}");
                    
                if (canvasGroup != null)
                    Debug.Log($"  CanvasGroup透明度: {canvasGroup.alpha}");
            }
        }

        private void OnUIUpdate(UIUpdateEvent eventData)
        {
            var uiElement = eventData.GetUIElement();
            var dataSource = eventData.GetDataSource();
            
            if (uiElement == null) return;
            
            switch (eventData.UpdateType)
            {
                case UIUpdateEvent.UIUpdateType.Text:
                    Debug.Log($"更新UI文本: {uiElement.name} -> {eventData.TextContent}");
                    break;
                    
                case UIUpdateEvent.UIUpdateType.Numeric:
                    Debug.Log($"更新UI数值: {uiElement.name} -> {eventData.NumericValue:P1}");
                    break;
            }
        }

        private void OnDestroy()
        {
            if (_uiInteractionHandler.IsValid)
                ZeroGCEventBus.Instance.Unsubscribe(_uiInteractionHandler);
                
            if (_uiUpdateHandler.IsValid)
                ZeroGCEventBus.Instance.Unsubscribe(_uiUpdateHandler);
        }
    }

    /// <summary>
    /// 资源管理器 - 展示资源扩展方法的使用
    /// </summary>
    public class AssetManager : MonoBehaviour
    {
        [SerializeField] private Material[] testMaterials;
        [SerializeField] private Texture2D[] testTextures;
        [SerializeField] private Sprite[] testSprites;
        [SerializeField] private AudioClip[] testAudioClips;
        
        private ZeroGCEventBus.EventHandlerId<AssetLoadEvent> _assetLoadHandler;

        private void Start()
        {
            _assetLoadHandler = ZeroGCEventBus.Instance.Subscribe<AssetLoadEvent>(OnAssetLoad);
            
            // 示例：预加载资源
            PreloadAssets();
        }

        private void PreloadAssets()
        {
            for (int i = 0; i < testMaterials.Length && i < testTextures.Length && i < testSprites.Length; i++)
            {
                // 使用新的资源扩展方法创建事件
                var loadEvent = AssetLoadEvent.Create(
                    testMaterials[i],
                    testTextures[i],
                    testSprites[i],
                    AssetLoadEvent.AssetLoadType.Preload,
                    1f
                );

                ZeroGCEventBus.Instance.Publish(loadEvent);
            }
        }

        private void OnAssetLoad(AssetLoadEvent eventData)
        {
            Debug.Log($"资源加载事件: {eventData.LoadType}, 进度: {eventData.LoadProgress:P1}");
            
            var material = eventData.GetMaterial();
            var texture = eventData.GetTexture();
            var sprite = eventData.GetSprite();
            var audioClip = eventData.GetAudioClip();
            
            if (material != null)
                Debug.Log($"  Material: {material.name}");
            if (texture != null)
                Debug.Log($"  Texture: {texture.name}");
            if (sprite != null)
                Debug.Log($"  Sprite: {sprite.name}");
            if (audioClip != null)
                Debug.Log($"  AudioClip: {audioClip.name}");
        }

        private void OnDestroy()
        {
            if (_assetLoadHandler.IsValid)
                ZeroGCEventBus.Instance.Unsubscribe(_assetLoadHandler);
        }
    }

    /// <summary>
    /// 场景管理器 - 展示新容器类型的使用
    /// </summary>
    public class SceneManager : MonoBehaviour
    {
        [SerializeField] private GameObject sceneRoot;
        [SerializeField] private GameObject[] staticObjects;
        [SerializeField] private Material[] sceneMaterials;
        [SerializeField] private AudioClip backgroundMusic;
        
        private ZeroGCEventBus.EventHandlerId<SceneDataEvent> _sceneDataHandler;
        private SceneData _currentSceneData;

        private void Start()
        {
            _sceneDataHandler = ZeroGCEventBus.Instance.Subscribe<SceneDataEvent>(OnSceneData);
            
            // 初始化场景数据
            InitializeSceneData();
        }

        private void InitializeSceneData()
        {
            _currentSceneData = new SceneData("ExampleScene");
            
            // 添加材质到字典
            for (int i = 0; i < sceneMaterials.Length; i++)
            {
                if (sceneMaterials[i] != null)
                    _currentSceneData.Materials[$"material_{i}"] = sceneMaterials[i];
            }
            
            _currentSceneData.BackgroundMusic = backgroundMusic;
            
            // 创建场景数据事件 - 展示新容器的使用
            var sceneEvent = SceneDataEvent.Create(
                _currentSceneData,
                sceneRoot,
                staticObjects,
                SceneDataEvent.SceneEventType.Load
            );
            
            ZeroGCEventBus.Instance.Publish(sceneEvent);
        }

        private void OnSceneData(SceneDataEvent eventData)
        {
            Debug.Log($"场景事件: {eventData.SceneName} - {eventData.EventType}");
            
            // 展示ReferenceIdDictionary的使用
            Debug.Log($"场景资源数量: {eventData.AssetReferences.Count}");
            var material0 = eventData.GetMaterial("material_0");
            if (material0 != null)
                Debug.Log($"  获取到材质: {material0.name}");
                
            var bgMusic = eventData.GetAudioClip("background_music");
            if (bgMusic != null)
                Debug.Log($"  背景音乐: {bgMusic.name}");
            
            // 展示HierarchyReferenceContainer的使用
            var sceneRoot = eventData.GetSceneRoot();
            var sceneObjects = eventData.GetSceneObjects();
            
            if (sceneRoot != null)
                Debug.Log($"  场景根节点: {sceneRoot.name}");
                
            Debug.Log($"  场景对象数量: {sceneObjects.Count}");
            foreach (var obj in sceneObjects)
            {
                if (obj != null)
                    Debug.Log($"    - {obj.name}");
            }
        }

        private void OnDestroy()
        {
            if (_sceneDataHandler.IsValid)
                ZeroGCEventBus.Instance.Unsubscribe(_sceneDataHandler);
        }
    }

    /// <summary>
    /// 技能系统示例 - 保持原有功能
    /// </summary>
    public class SkillSystem : MonoBehaviour
    {
        [SerializeField] private ScriptableObject lightningSkill;
        [SerializeField] private float skillRange = 5f;
        [SerializeField] private int maxTargets = 3;
        
        private ZeroGCEventBus.EventHandlerId<MultiTargetSkillEvent> _skillEventHandler;

        private void Start()
        {
            _skillEventHandler = ZeroGCEventBus.Instance.Subscribe<MultiTargetSkillEvent>(OnMultiTargetSkill);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                CastLightningSkill();
            }
        }

        private void CastLightningSkill()
        {
            // 寻找范围内的敌人
            var colliders = Physics.OverlapSphere(transform.position, skillRange);
            var targets = new List<GameObject>();
            
            foreach (var collider in colliders)
            {
                if (collider.CompareTag("Enemy") && targets.Count < maxTargets)
                {
                    targets.Add(collider.gameObject);
                }
            }
            
            if (targets.Count > 0)
            {
                // 创建并发布多目标技能事件
                var skillEvent = MultiTargetSkillEvent.Create(
                    gameObject,
                    lightningSkill,
                    targets.ToArray(),
                    transform.position,
                    50f,
                    Random.value < 0.3f // 30% 暴击率
                );
                
                ZeroGCEventBus.Instance.Publish(skillEvent);
            }
        }

        private void OnMultiTargetSkill(MultiTargetSkillEvent eventData)
        {
            var caster = eventData.GetCaster();
            var skill = eventData.GetSkillData();
            var targets = eventData.GetValidTargets(); // 自动过滤已被销毁的目标
            
            if (caster != null && targets.Count > 0)
            {
                string critText = eventData.IsCritical ? " (暴击!)" : "";
                Debug.Log($"{caster.name} 对 {targets.Count} 个目标释放了 {skill?.name}，威力: {eventData.SkillPower}{critText}");
                
                // 对每个有效目标造成伤害
                foreach (var target in targets)
                {
                    if (target != null)
                    {
                        Debug.Log($"  -> {target.name} 受到了 {eventData.SkillPower} 点伤害");
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (_skillEventHandler.IsValid)
                ZeroGCEventBus.Instance.Unsubscribe(_skillEventHandler);
        }
    }

    #endregion

    #region 性能测试

    /// <summary>
    /// 引用类型性能测试 - 更新为测试新扩展方法
    /// </summary>
    public class ReferenceTypePerformanceTest : MonoBehaviour
    {
        [SerializeField] private int testObjectCount = 1000;
        [SerializeField] private int testIterations = 100;

        /// <summary>
        /// 安全销毁对象的辅助方法
        /// </summary>
        private void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            
#if UNITY_EDITOR
            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
#else
            Destroy(obj);
#endif
        }
        
        private void Start()
        {
            StartCoroutine(RunPerformanceTests());
        }
        
        private System.Collections.IEnumerator RunPerformanceTests()
        {
            yield return new WaitForSeconds(1f);
            
            Debug.Log("开始引用类型性能测试...");
            
            // 测试1: ID创建性能
            TestIdCreationPerformance();
            yield return new WaitForSeconds(0.1f);
            
            // 测试2: ID检索性能
            TestIdRetrievalPerformance();
            yield return new WaitForSeconds(0.1f);
            
            // 测试3: 新扩展方法性能
            TestNewExtensionsPerformance();
            yield return new WaitForSeconds(0.1f);
            
            // 测试4: 容器类型性能
            TestContainerPerformance();
            yield return new WaitForSeconds(0.1f);
            
            // 测试5: 事件发布性能
            TestEventPublishingPerformance();
            yield return new WaitForSeconds(0.1f);
            
            // 测试6: 内存清理性能
            TestCleanupPerformance();
            
            Debug.Log("引用类型性能测试完成！");
        }
        
        private void TestIdCreationPerformance()
        {
            var testObjects = new GameObject[testObjectCount];
            for (int i = 0; i < testObjectCount; i++)
            {
                testObjects[i] = new GameObject($"TestObject_{i}");
            }
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            for (int iteration = 0; iteration < testIterations; iteration++)
            {
                for (int i = 0; i < testObjectCount; i++)
                {
                    testObjects[i].ToReferenceId();
                }
            }
            
            stopwatch.Stop();
            
            Debug.Log($"ID创建性能测试: {testObjectCount * testIterations} 次操作耗时 {stopwatch.ElapsedMilliseconds} ms");
            
            // 清理测试对象
            for (int i = 0; i < testObjectCount; i++)
            {
                SafeDestroy(testObjects[i]);
            }
        }
        
        private void TestIdRetrievalPerformance()
        {
            var testObjects = new GameObject[testObjectCount];
            var testIds = new int[testObjectCount];
            
            for (int i = 0; i < testObjectCount; i++)
            {
                testObjects[i] = new GameObject($"TestObject_{i}");
                testIds[i] = testObjects[i].ToReferenceId();
            }
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            for (int iteration = 0; iteration < testIterations; iteration++)
            {
                for (int i = 0; i < testObjectCount; i++)
                {
                    var retrieved = ReferenceTypeManager.GetReference<GameObject>(testIds[i]);
                }
            }
            
            stopwatch.Stop();
            
            Debug.Log($"ID检索性能测试: {testObjectCount * testIterations} 次操作耗时 {stopwatch.ElapsedMilliseconds} ms");
            
            // 清理测试对象
            for (int i = 0; i < testObjectCount; i++)
            {
                SafeDestroy(testObjects[i]);
            }
        }

        private void TestNewExtensionsPerformance()
        {
            var testObjects = new GameObject[testObjectCount];
            var materials = new Material[testObjectCount];
            
            // 创建测试对象和资源
            for (int i = 0; i < testObjectCount; i++)
            {
                testObjects[i] = new GameObject($"TestObject_{i}");
                testObjects[i].AddComponent<Rigidbody>();
                testObjects[i].AddComponent<BoxCollider>();
                
                materials[i] = new Material(Shader.Find("Standard"));
            }
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // 测试新扩展方法
            for (int iteration = 0; iteration < testIterations; iteration++)
            {
                for (int i = 0; i < testObjectCount; i++)
                {
                    // 测试Transform扩展
                    testObjects[i].transform.ToReferenceId();
                    
                    // 测试物理组件扩展
                    var rb = testObjects[i].GetComponent<Rigidbody>();
                    var col = testObjects[i].GetComponent<Collider>();
                    rb.ToPhysicsReferenceId();
                    col.ToPhysicsReferenceId();
                    
                    // 测试资源扩展
                    materials[i].ToAssetReferenceId();
                }
            }
            
            stopwatch.Stop();
            
            Debug.Log($"新扩展方法性能测试: {testObjectCount * testIterations * 4} 次操作耗时 {stopwatch.ElapsedMilliseconds} ms");
            
            // 清理
            for (int i = 0; i < testObjectCount; i++)
            {
                SafeDestroy(testObjects[i]);
                SafeDestroy(materials[i]);
            }
        }

        private void TestContainerPerformance()
        {
            var testObjects = new GameObject[testObjectCount];
            for (int i = 0; i < testObjectCount; i++)
            {
                testObjects[i] = new GameObject($"TestObject_{i}");
            }
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            for (int iteration = 0; iteration < testIterations; iteration++)
            {
                // 测试ReferenceIdContainer
                var container = new ReferenceIdContainer(
                    testObjects[0].ToReferenceId(),
                    testObjects[1].ToReferenceId(),
                    testObjects[2].ToReferenceId()
                );
                
                var primary = container.GetPrimary<GameObject>();
                var secondary = container.GetSecondary<GameObject>();
                
                // 测试ReferenceIdArray
                var smallArray = new GameObject[10];
                for (int j = 0; j < 10 && j < testObjects.Length; j++)
                {
                    smallArray[j] = testObjects[j];
                }
                var array = new ReferenceIdArray(smallArray);
                var references = array.GetReferences<GameObject>();
                
                // 测试ReferenceIdDictionary
                var dict = new Dictionary<string, object>
                {
                    { "obj1", testObjects[0] },
                    { "obj2", testObjects[1] }
                };
                var refDict = new ReferenceIdDictionary(dict);
                var obj1 = refDict.GetReference<GameObject>("obj1");
            }
            
            stopwatch.Stop();
            
            Debug.Log($"容器类型性能测试: {testIterations} 次操作耗时 {stopwatch.ElapsedMilliseconds} ms");
            
            // 清理
            for (int i = 0; i < testObjectCount; i++)
            {
                SafeDestroy(testObjects[i]);
            }
        }
        
        private void TestEventPublishingPerformance()
        {
            var attacker = new GameObject("Attacker");
            var target = new GameObject("Target");
            var weapon = ScriptableObject.CreateInstance<Weapon>();
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            for (int i = 0; i < testIterations * 10; i++)
            {
                // 测试使用新扩展方法的事件创建
                var attackEvent = PlayerAttackEvent.Create(
                    attacker, target, weapon,
                    Vector3.zero, 10f
                );
                
                ZeroGCEventBus.Instance.Publish(attackEvent);
            }
            
            stopwatch.Stop();
            
            Debug.Log($"事件发布性能测试: {testIterations * 10} 次操作耗时 {stopwatch.ElapsedMilliseconds} ms");
            
            SafeDestroy(attacker);
            SafeDestroy(target);
            SafeDestroy(weapon);
        }
        
        private void TestCleanupPerformance()
        {
            // 创建一些对象然后销毁它们，触发弱引用失效
            for (int i = 0; i < testObjectCount; i++)
            {
                var obj = new GameObject($"TempObject_{i}");
                obj.ToReferenceId();
                obj.transform.ToReferenceId();
                SafeDestroy(obj);
            }
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            int cleanedCount = ReferenceTypeManager.CleanupDeadReferences(true);
            stopwatch.Stop();
            
            Debug.Log($"内存清理性能测试: 清理了 {cleanedCount} 个死亡引用，耗时 {stopwatch.ElapsedMilliseconds} ms");
            
            // 打印统计信息
            ReferenceTypeManager.LogPerformanceStats();
        }
    }

    #endregion
} 