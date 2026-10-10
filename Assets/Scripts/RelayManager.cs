using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }

    [Header("Room Info")]
    public string currentJoinCode = "";
    public int maxPlayers = 4;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private async void Start()
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            await UnityServices.InitializeAsync();
        }
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    public async Task<string> CreateRelayRoom()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            currentJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
            utp.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();
            return currentJoinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e.Message);
            return null;
        }
    }

    public async Task<bool> JoinRelayRoom(string joinCode)
    {
        try
        {
            string cleanCode = joinCode.Trim().ToUpper();
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(cleanCode);

            var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
            utp.SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            return NetworkManager.Singleton.StartClient();
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
            return false;
        }
    }
}