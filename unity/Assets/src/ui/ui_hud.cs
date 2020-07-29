using UnityEngine.UI;
using UnityEngine;
using TMPro;

namespace game
{
  public class UIHud : UIWindow
  {
    public static readonly string PREFAB = "ui/ui_hud";

    Slider hp_bar;
    TextMeshProUGUI enemies_count_txt;
    TextMeshProUGUI wave_txt;
    Animator wave_txt_animator;

    public override void Init()
    {
      hp_bar = GetUIComponent<Slider>("hp_bar");
      Error.Verify(hp_bar != null);

      enemies_count_txt = GetUIComponent<TextMeshProUGUI>("enemies_count_txt");
      Error.Verify(enemies_count_txt != null);

      wave_txt = GetUIComponent<TextMeshProUGUI>("wave_txt");
      Error.Verify(wave_txt != null);

      wave_txt_animator = wave_txt.gameObject.GetComponent<Animator>();
      Error.Verify(wave_txt_animator != null);
    }

    public void UpdateHPBar(float value)
    {
      hp_bar.value = value;
    }

    public void UpdateEnemiesCountText(uint killed, uint all)
    {
      string text = string.Format("Убито: {0}/{1}", killed, all);
      enemies_count_txt.text = text;
    }

    public void ShowWaveText(uint wave_idx)
    {
      string text = wave_idx + " волна!";
      wave_txt.text = text;

      wave_txt_animator.gameObject.SetActive(true);
      wave_txt_animator.Rebind();
      wave_txt_animator.Play("WaveText");
    }
  }
}