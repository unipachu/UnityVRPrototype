using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Used to pass player input to ECS subscene.
/// </summary>
// TODO: Make this run as early as possible so that ECS gets input as early as possible.
public class EcsPlrCtrlBridge : MonoBehaviour {
    [Header("Settings")]
    [Tooltip("Unique player id of the EcsPlrInput.")]
    [SerializeField] int tgtEcsPlrCtrlId;
    
    [Header("Input Action Refs")]
    [SerializeField] InputActionProperty lGrbInputAct;
    [SerializeField] InputActionProperty rGrbInputAct;
    [SerializeField] InputActionProperty lTriggerInputAct;
    [SerializeField] InputActionProperty rTriggerInputAct;

    EntityManager entityMgr;
    Entity tgtEntity;
    bool tgtFound;

    private void Start() {
        entityMgr = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    void Update() {
        if (!tgtFound) {
            FindTgtEcsPlrCtrlComponent();
            if (!tgtFound)
                return;
        }
        float lGrbValue = lGrbInputAct.action.ReadValue<float>();
        float rGrbValue = rGrbInputAct.action.ReadValue<float>();
        float lTrgValue = lTriggerInputAct.action.ReadValue<float>();
        float rTrgValue = rTriggerInputAct.action.ReadValue<float>();
        entityMgr.SetComponentData(
            tgtEntity,
            new EcsPlrInput {
                plrId = tgtEcsPlrCtrlId,
                lGrbValue = lGrbValue,
                lGrbPressed = lGrbValue >= 0.5f,
                rGrbValue = rGrbValue,
                rGrbPressed = rGrbValue >= 0.5f,
                lTrgValue = lTrgValue,
                lTrgPressed = lTrgValue >= 0.5f,
                rTrgValue = rTrgValue,
                rTrgPressed = rTrgValue >= 0.5f,
            }
        );
    }

    private void FindTgtEcsPlrCtrlComponent() {
        EntityQuery query = entityMgr.CreateEntityQuery(typeof(EcsPlrInput));
        NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        foreach (Entity entity in entities) {
            EcsPlrInput tgtPlrCtrl = entityMgr.GetComponentData<EcsPlrInput>(entity);
            if (tgtPlrCtrl.plrId == tgtEcsPlrCtrlId) {
                tgtEntity = entity;
                tgtFound = true;
                break;
            }
        }
        entities.Dispose();
    }
}
