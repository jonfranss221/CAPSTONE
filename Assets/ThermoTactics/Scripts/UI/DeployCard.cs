using System;
using UnityEngine;
using UnityEngine.UI;

namespace ThermoTactics
{
    /// <summary>One card in the deploy bar: portrait and OP cost badge (the name shows in the hint line).</summary>
    public class DeployCard : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private Image _frame;
        [SerializeField] private Text _name;
        [SerializeField] private Text _cost;

        private static readonly Color SelectedFrame = new Color(0.886f, 0.682f, 0.173f);   // #E2AE2C
        private static readonly Color NormalFrame = new Color(0.231f, 0.329f, 0.275f);     // #3B5446

        public UnitData Data { get; private set; }

        public void Setup(UnitData data, Action<UnitData> onClick)
        {
            Data = data;
            gameObject.SetActive(data != null);
            if (data == null) return;
            _icon.sprite = data.card;
            _icon.preserveAspect = true;
            if (_name != null) _name.text = data.displayName;
            _cost.text = data.opCost.ToString();
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onClick(data));
        }

        public void Refresh(bool selected, bool affordable, bool interactable)
        {
            if (Data == null) return;
            _frame.color = selected ? SelectedFrame : NormalFrame;
            _icon.color = affordable ? Color.white : new Color(0.4f, 0.4f, 0.4f);
            _cost.color = affordable ? new Color(0.608f, 0.827f, 0.69f) : new Color(1f, 0.45f, 0.4f);
            _button.interactable = interactable;
        }
    }
}
