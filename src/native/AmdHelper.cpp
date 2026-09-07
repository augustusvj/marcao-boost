#include <iostream>
#include <string>
#include "SDK/ADLXHelper/Windows/Cpp/ADLXHelper.h"
#include "SDK/Include/I3DSettings.h"

using namespace adlx;

template<class T> static int change(T feature, bool desired)
{
    adlx_bool supported = false, previous = false;
    ADLX_RESULT result = feature->IsSupported(&supported); if (!ADLX_SUCCEEDED(result) || !supported) { std::cerr << "ERRO|A configuração não é suportada por esta GPU AMD." << std::endl; return 6; }
    result = feature->IsEnabled(&previous); if (!ADLX_SUCCEEDED(result)) { std::cerr << "ERRO|Não foi possível ler a configuração atual." << std::endl; return 7; }
    result = feature->SetEnabled(desired); if (!ADLX_SUCCEEDED(result)) { std::cerr << "ERRO|O driver AMD recusou a alteração. Código " << result << std::endl; return 8; }
    std::cout << "OK|" << (previous ? "1" : "0") << std::endl; return 0;
}

int main(int argc, char** argv)
{
    if (argc < 4) { std::cerr << "ERRO|Comando inválido." << std::endl; return 2; }
    const std::string key = argv[2]; const bool desired = std::string(argv[3]) == "1"; ADLXHelper helper;
    ADLX_RESULT result = helper.Initialize(); if (!ADLX_SUCCEEDED(result)) { std::cerr << "ERRO|AMD ADLX não está disponível. Atualize o AMD Software: Adrenalin Edition." << std::endl; return 3; }
    IADLXGPUListPtr gpus; IADLX3DSettingsServicesPtr settings; result = helper.GetSystemServices()->GetGPUs(&gpus);
    if (ADLX_SUCCEEDED(result)) result = helper.GetSystemServices()->Get3DSettingsServices(&settings);
    if (!ADLX_SUCCEEDED(result) || gpus == nullptr || gpus->Empty()) { std::cerr << "ERRO|Nenhuma GPU AMD compatível foi encontrada." << std::endl; return 4; }
    IADLXGPUPtr gpu; result = gpus->At(0, &gpu); int code = 5;
    if (ADLX_SUCCEEDED(result) && key == "antilag") { IADLX3DAntiLagPtr f; result = settings->GetAntiLag(gpu, &f); if (ADLX_SUCCEEDED(result)) code = change(f, desired); }
    else if (ADLX_SUCCEEDED(result) && key == "chill") { IADLX3DChillPtr f; result = settings->GetChill(gpu, &f); if (ADLX_SUCCEEDED(result)) code = change(f, desired); }
    else if (ADLX_SUCCEEDED(result) && key == "boost") { IADLX3DBoostPtr f; result = settings->GetBoost(gpu, &f); if (ADLX_SUCCEEDED(result)) code = change(f, desired); }
    else if (ADLX_SUCCEEDED(result) && key == "enhancedsync") { IADLX3DEnhancedSyncPtr f; result = settings->GetEnhancedSync(gpu, &f); if (ADLX_SUCCEEDED(result)) code = change(f, desired); }
    else if (ADLX_SUCCEEDED(result) && key == "frtc") { IADLX3DFrameRateTargetControlPtr f; result = settings->GetFrameRateTargetControl(gpu, &f); if (ADLX_SUCCEEDED(result)) code = change(f, desired); }
    if (code == 5) std::cerr << "ERRO|A configuração não existe ou não é suportada pelo driver AMD." << std::endl;
    return code;
}
