using SiberianGJ26.YouAreDoing.Antos.Singleton;
using UnityEngine;

namespace SiberianGJ26.YouAreDoing.Antos
{
    public class ObjectTracking : WorldPoint
    {
        [field: SerializeField] public GameObject Prefab { get; private set; }
        
        private GameObject _item;

        //Singleton
        private TrackItems _trackItems;

        private void Start()
        {
            _trackItems = TrackItems.Instance;
            _trackItems.Add(this);
        }

        private void OnDestroy()
        {
            _trackItems?.Remove(this);
        }

        public void Init(GameObject newItem)
        {
            _item = newItem;
            Prefab.gameObject.SetActive(false);
            _item.gameObject.SetActive(true);
        }
        
        public bool IsItemTake()
        {
            return _item == null;
        }
    }
}