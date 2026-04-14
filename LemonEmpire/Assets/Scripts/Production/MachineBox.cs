using UnityEngine;
using LemonEmpire.Core;

namespace LemonEmpire.Production
{

    public class MachineBox : ItemBase
    {
        [Header("Machine Box")]
        [SerializeField] private GameObject machinePrefab;
        [SerializeField] private string machineName = "Станок";

        public override string InteractionPrompt
        {
            get
            {

                if (GetComponent<Rigidbody>() != null && !GetComponent<Rigidbody>().isKinematic)
                {
                    return $"[E] Подобрать | [F] Распаковать {machineName}";
                }
                return base.InteractionPrompt;
            }
        }

        public void SetupBox(GameObject prefab, string nameStr)
        {
            machinePrefab = prefab;
            machineName = nameStr;
            Setup(ItemType.EquipmentBox, $"Коробка: {nameStr}", 100f);
        }

        public void Unpack()
        {
            if (machinePrefab != null)
            {

                float surfaceY = transform.position.y;
                var boxCol = GetComponent<Collider>();
                if (boxCol != null)
                    surfaceY = boxCol.bounds.min.y;

                var machine = Instantiate(machinePrefab, transform.position, transform.rotation);

                var machineCol = machine.GetComponent<Collider>();
                if (machineCol != null)
                {

                    Physics.SyncTransforms();

                    float distToBottom = machine.transform.position.y - machineCol.bounds.min.y;
                    machine.transform.position = new Vector3(machine.transform.position.x, surfaceY + distToBottom, machine.transform.position.z);
                }
            }

            Consume();
        }
    }
}
