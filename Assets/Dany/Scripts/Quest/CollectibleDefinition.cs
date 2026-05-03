using UnityEngine;

namespace Dany
{
    /// <summary>Тип коллекционного предмета для квестов (не кладётся в слоты оружия).</summary>
    [CreateAssetMenu(fileName = "Collectible", menuName = "Dany/Quest/Collectible Definition")]
    public class CollectibleDefinition : ScriptableObject
    {
        [Tooltip("Имя в подсказке и в панели задач.")]
        public string displayName;
    }
}
