using UnityEngine;
using TMPro;
using PurrNet.Prediction;

/// <summary>
/// Simple script to display a player's name based on their PlayerID.
/// Inherits from StatelessPredictedIdentity to automatically sync its owner property.
/// </summary>
public class PlayerNameDisplay : StatelessPredictedIdentity
{
    [SerializeField] private TMP_Text _nameText;

    protected override void LateAwake()
    {
        base.LateAwake();
        
        if (_nameText == null)
            _nameText = GetComponent<TMP_Text>();
            
        UpdateName();
    }

    private void Update()
    {
        // Continuously update in case the name arrives late (e.g. Steam data sync)
        UpdateName();
    }

    public void UpdateName()
    {
        if (_nameText == null || !owner.HasValue) return;

        if (MatchSessionManager.Instance != null)
        {
            // MatchSessionManager.GetPlayerName returns "Player {id}" as a default 
            // if no Steam name is registered yet.
            _nameText.text = MatchSessionManager.Instance.GetPlayerName(owner.Value);
        }
        else
        {
            _nameText.text = $"Player {owner.Value.id}";
        }
    }
}
