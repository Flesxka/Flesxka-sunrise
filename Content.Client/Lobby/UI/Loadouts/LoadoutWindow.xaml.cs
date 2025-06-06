using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Shared.CCVar;
using Content.Shared.Dataset;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Random.Helpers;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Sunrise.Interfaces.Shared; // Sunrise-Sponsors

namespace Content.Client.Lobby.UI.Loadouts;

[GenerateTypedNameReferences]
public sealed partial class LoadoutWindow : FancyWindow
{
    public event Action<string>? OnNameChanged;
    public event Action<ProtoId<LoadoutGroupPrototype>, ProtoId<LoadoutPrototype>>? OnLoadoutPressed;
    public event Action<ProtoId<LoadoutGroupPrototype>, ProtoId<LoadoutPrototype>>? OnLoadoutUnpressed;

    private List<LoadoutGroupContainer> _groups = new();

    public HumanoidCharacterProfile Profile;

    // CCvar.
    private int _maxLoadoutNameLength;

    public LoadoutWindow(HumanoidCharacterProfile profile, RoleLoadout loadout, RoleLoadoutPrototype proto, ICommonSession session, IDependencyCollection collection, ISharedSponsorsManager? sponsorsManager)  // Sunrise-Sponsors
    {
        RobustXamlLoader.Load(this);
        Profile = profile;
        var protoManager = collection.Resolve<IPrototypeManager>();
        var configManager = collection.Resolve<IConfigurationManager>();

        _maxLoadoutNameLength = configManager.GetCVar(CCVars.MaxLoadoutNameLength);
        RoleNameEdit.IsValid = text => text.Length <= _maxLoadoutNameLength;

        // Hide if we can't edit the name.
        if (!proto.CanCustomizeName)
        {
            RoleNameBox.Visible = false;
        }
        else
        {
            var name = loadout.EntityName;

            LoadoutNameLabel.Text = proto.NameDataset == null ?
                Loc.GetString("loadout-name-edit-label") :
                Loc.GetString("loadout-name-edit-label-dataset");

            RoleNameEdit.ToolTip = Loc.GetString(
                "loadout-name-edit-tooltip",
                ("max", _maxLoadoutNameLength));
            RoleNameEdit.Text = name ?? string.Empty;
            RoleNameEdit.OnTextChanged += args => OnNameChanged?.Invoke(args.Text);
        }

        // Hide if no groups
        if (proto.Groups.Count == 0)
        {
            LoadoutGroupsContainer.Visible = false;
            SetSize = Vector2.Zero;
        }
        else
        {
            foreach (var group in proto.Groups)
            {
                if (!protoManager.TryIndex(group, out var groupProto))
                    continue;

                if (groupProto.Hidden)
                    continue;

                var container = new LoadoutGroupContainer(profile, loadout, protoManager.Index(group), session, collection, sponsorsManager);  // Sunrise-Sponsors
                LoadoutGroupsContainer.AddTab(container, Loc.GetString(groupProto.Name));
                _groups.Add(container);

                container.OnLoadoutPressed += args =>
                {
                    OnLoadoutPressed?.Invoke(group, args);
                };

                container.OnLoadoutUnpressed += args =>
                {
                    OnLoadoutUnpressed?.Invoke(group, args);
                };
            }
        }
    }

    public void RefreshLoadouts(RoleLoadout loadout, ICommonSession session, IDependencyCollection collection)
    {
        foreach (var group in _groups)
        {
            group.RefreshLoadouts(Profile, loadout, session, collection);
        }
    }
}
