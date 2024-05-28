using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Mimir.Worker.Models;
using Nekoyume.Model;
using Nekoyume.Model.State;

namespace Mimir.Worker.Handler;

public class WorldInformationStateHandler : IStateHandler<StateData>
{
    private class WorldInformationState : State
    {
        public WorldInformation WorldInformation;

        public WorldInformationState(Address address, WorldInformation worldInformation)
            : base(address)
        {
            WorldInformation = worldInformation;
        }
    }

    public StateData ConvertToStateData(Address address, IValue rawState)
    {
        var worldInformation = ConvertToState(rawState);
        return new StateData(address, new WorldInformationState(address, worldInformation));
    }

    public StateData ConvertToStateData(Address address, string rawState)
    {
        Codec Codec = new();
        var state = Codec.Decode(Convert.FromHexString(rawState));
        var worldInformation = ConvertToState(state);

        return new StateData(address, new WorldInformationState(address, worldInformation));
    }

    private WorldInformation ConvertToState(IValue state)
    {
        if (state is Dictionary dict)
        {
            return new WorldInformation(dict);
        }
        else
        {
            throw new ArgumentException("Invalid state type. Expected Dictionary.", nameof(state));
        }
    }
}
