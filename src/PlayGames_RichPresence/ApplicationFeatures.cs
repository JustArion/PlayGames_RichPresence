using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData.Binding;

namespace Dawn.PlayGames.RichPresence;

using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

/// <summary>
/// Application features differs from LaunchArgs as LaunchArgs is immutable
/// </summary>
internal partial class ApplicationFeatures : ObservableObject
{
    public ApplicationFeatures()
    {
        this.WhenPropertyChanged(x => x.RichPresenceEnabled)
            .Subscribe(value =>
                Log.Verbose($"ApplicationFeature changed {nameof(RichPresenceEnabled)} ({{Value}})", value.Value));

        this.WhenPropertyChanged(x => CheckPreReleases)
            .Subscribe(value =>
                Log.Verbose($"ApplicationFeature changed {nameof(CheckPreReleases)} ({{Value}})", value.Value));
    }

    public void Sync()
    {
        CheckPreReleases = Arguments.CheckPreReleases;
        RichPresenceEnabled = Arguments.RichPresenceEnabledOnStart;
    }

    [ObservableProperty]
    [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
    public partial bool RichPresenceEnabled { get; set; }

    [ObservableProperty]
    [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
    public partial bool CheckPreReleases { get; set; }
}
