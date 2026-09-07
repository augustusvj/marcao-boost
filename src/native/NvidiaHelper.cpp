#include <windows.h>
#include <iostream>
#include <string>
#include <cstring>
#include "nvapi.h"
#include "NvApiDriverSettings.h"

static void fail(const std::string& message, NvAPI_Status status = NVAPI_ERROR)
{
    NvAPI_ShortString detail = {};
    if (status != NVAPI_ERROR) NvAPI_GetErrorMessage(status, detail);
    std::cerr << "ERRO|" << message;
    if (detail[0]) std::cerr << " (" << detail << ")";
    std::cerr << std::endl;
}

static bool setting(const std::string& key, NvU32& id, NvU32& recommended)
{
    if (key == "power") { id = PREFERRED_PSTATE_ID; recommended = PREFERRED_PSTATE_PREFER_MAX; return true; }
    if (key == "texture") { id = QUALITY_ENHANCEMENTS_ID; recommended = QUALITY_ENHANCEMENTS_HIGHPERFORMANCE; return true; }
    if (key == "shader") { id = PS_SHADERDISKCACHE_ID; recommended = PS_SHADERDISKCACHE_ON; return true; }
    if (key == "latency")
    {
        NvAPI_UnicodeString name = {}; const wchar_t* value = L"Low Latency Mode";
        std::memcpy(name, value, (wcslen(value) + 1) * sizeof(wchar_t));
        recommended = 1; return NvAPI_DRS_GetSettingIdFromName(name, &id) == NVAPI_OK;
    }
    return false;
}

int main(int argc, char** argv)
{
    if (argc < 3) { fail("Comando inválido."); return 2; }
    NvAPI_Status status = NvAPI_Initialize(); if (status != NVAPI_OK) { fail("Driver NVIDIA não encontrado.", status); return 3; }
    NvDRSSessionHandle session = nullptr; NvDRSProfileHandle profile = nullptr;
    status = NvAPI_DRS_CreateSession(&session); if (status != NVAPI_OK) { fail("Não foi possível iniciar o perfil NVIDIA.", status); return 4; }
    status = NvAPI_DRS_LoadSettings(session); if (status == NVAPI_OK) status = NvAPI_DRS_GetBaseProfile(session, &profile);
    if (status != NVAPI_OK) { fail("Não foi possível carregar o perfil NVIDIA.", status); NvAPI_DRS_DestroySession(session); return 5; }
    NvU32 settingId = 0, recommended = 0; if (!setting(argv[2], settingId, recommended)) { fail("A configuração não é suportada por este driver."); NvAPI_DRS_DestroySession(session); return 6; }
    std::string command = argv[1], oldValue = argc > 3 ? argv[3] : ""; NVDRS_SETTING current = {}; current.version = NVDRS_SETTING_VER;
    NvAPI_Status get = NvAPI_DRS_GetSetting(session, profile, settingId, &current);
    std::string backup = get == NVAPI_OK && current.settingType == NVDRS_DWORD_TYPE ? std::to_string(current.u32CurrentValue) : "default";
    if (command == "restore" && oldValue == "default") status = NvAPI_DRS_RestoreProfileDefaultSetting(session, profile, settingId);
    else
    {
        NvU32 desired = command == "set" ? recommended : static_cast<NvU32>(std::stoul(oldValue));
        NVDRS_SETTING value = {}; value.version = NVDRS_SETTING_VER; value.settingId = settingId; value.settingType = NVDRS_DWORD_TYPE; value.u32CurrentValue = desired;
        status = NvAPI_DRS_SetSetting(session, profile, &value);
    }
    if (status == NVAPI_OK) status = NvAPI_DRS_SaveSettings(session);
    NvAPI_DRS_DestroySession(session); NvAPI_Unload();
    if (status != NVAPI_OK) { fail("O driver recusou a alteração.", status); return 7; }
    std::cout << "OK|" << backup << std::endl; return 0;
}
