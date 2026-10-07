using System;
using UnityEngine;
using UnityEngine.UI;

namespace ThermoTactics
{
    /// <summary>One card in the deploy bar: portrait, name and OP cost.</summary>
    public class DeployCard : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private Image _frame;
        [SerializeField] private Text _name;
        [SerializeField] private Text _cost;

        private static readonly Color SelectedFrame = new Color(1f, 0.82f, 0.25f);
        private static readonly Color NormalFrame = new Color(0.12f, 0.14f, 0.18f, 0.95f);

        public UnitData Data { get; private set; }

        public void Setup(UnitData data, Action<UnitData> onClick)
        {
            Data = data;
            gameObject.SetActive(data != null);
            if (data == null) return;
            _icon.sprite = data.card;
            _icon.preserveAspect = true;
            _name.text = data.displayName;
            _cost.text = $"{data.opCost} OP";
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onClick(data));
        }

        public void Refresh(bool selected, bool affordable, bool interactable)
        {
            if (Data == null) return;
            _frame.color = selected ? SelectedFrame : NormalFrame;
            _icon.color = affordable ? Color.white : new Color(0.45f, 0.45f, 0.45f);
            _cost.color = affordable ? new Color(0.55f, 1f, 0.65f) : new Color(1f, 0.45f, 0.4f);
            _button.interactable = interactable;
        }
    }
}
