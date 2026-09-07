using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using System.Reflection;

[assembly: AssemblyTitle("Marcão Boost")]
[assembly: AssemblyProduct("Marcão Boost")]
[assembly: AssemblyDescription("Otimizador conectado e reversível para Windows")]
[assembly: AssemblyVersion("2.3.0.0")]
[assembly: AssemblyFileVersion("2.3.0.0")]

namespace MarcaoBoostWpf
{
    static class ClientEntry
    {
        [STAThread]
        public static void Main()
        {
            Application app = new Application(); app.ShutdownMode = ShutdownMode.OnLastWindowClose; ClientLoginWindow login=new ClientLoginWindow();app.MainWindow=login;app.Run(login);
        }
    }

    sealed class ClientLoginWindow : Window
    {
        readonly Grid formHost = new Grid(); bool registerMode;
        public ClientLoginWindow()
        {
            Ui.ConfigureWindow(this, "Marcão Boost — Acesso", 1060, 680);
            Grid root = new Grid(); root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44, GridUnitType.Star) }); root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56, GridUnitType.Star) }); Content = Ui.Chrome(this, root);
            Border brand = new Border { Background = Ui.Brush(Ui.Nav), Padding = new Thickness(54) }; Grid.SetColumn(brand, 0); root.Children.Add(brand);
            StackPanel b = new StackPanel { VerticalAlignment = VerticalAlignment.Center }; b.Children.Add(Ui.BrandImage(150));
            TextBlock name = Ui.TextBlock("MARCÃO\nBOOST", 34, Ui.Lime, FontWeights.Black); name.TextAlignment = TextAlignment.Center; name.Margin = new Thickness(0, 24, 0, 18); b.Children.Add(name);
            TextBlock desc = Ui.TextBlock("Seu Windows mais rápido, com cada ajuste explicado e reversível.", 16, Ui.Muted, FontWeights.Normal); desc.TextAlignment = TextAlignment.Center; b.Children.Add(desc); brand.Child = b;
            Border right = new Border { Padding = new Thickness(76, 48, 76, 48), Background = Ui.Brush(Ui.Bg) }; Grid.SetColumn(right, 1); root.Children.Add(right); right.Child = formHost; ShowForm(false);
        }
        void ShowForm(bool register)
        {
            registerMode = register; formHost.Children.Clear(); StackPanel s = new StackPanel { VerticalAlignment = VerticalAlignment.Center, MaxWidth = 410 };
            s.Children.Add(Ui.TextBlock(register ? "Criar sua conta" : "Bem-vindo de volta", 30, Ui.Text, FontWeights.SemiBold));
            TextBlock sub = Ui.TextBlock(register ? "O administrador precisa aprovar o cadastro antes do primeiro acesso." : "Entre para acessar as otimizações liberadas para sua conta.", 14, Ui.Muted, FontWeights.Normal); sub.Margin = new Thickness(0, 8, 0, 28); s.Children.Add(sub);
            TextBox name = new TextBox(); if (register) s.Children.Add(Ui.Field(name, "NOME"));
            TextBox email = new TextBox(); PasswordBox password = new PasswordBox(); s.Children.Add(Ui.Field(email, "E-MAIL")); s.Children.Add(Ui.Field(password, "SENHA"));
            TextBlock message = Ui.TextBlock("", 13, Ui.Amber, FontWeights.Normal); message.Margin = new Thickness(4, 0, 0, 14); s.Children.Add(message);
            Button primary = Ui.Button(register ? "Enviar cadastro" : "Entrar", true, 410); primary.HorizontalAlignment = HorizontalAlignment.Stretch; s.Children.Add(primary);
            Button toggle = Ui.Button(register ? "Já tenho uma conta" : "Criar uma conta", false, 410); toggle.HorizontalAlignment = HorizontalAlignment.Stretch; toggle.Margin = new Thickness(0, 12, 0, 0); toggle.Click += delegate { ShowForm(!registerMode); }; s.Children.Add(toggle);
            primary.Click += async delegate
            {
                primary.IsEnabled = false; message.Text = "Conectando...";
                ApiResult result = register ? await Api.Post("/auth/register", new { name = name.Text, email = email.Text, password = password.Password }, null) : await Api.Post("/auth/login", new { email = email.Text, password = password.Password }, null);
                primary.IsEnabled = true;
                if (!result.Ok) { message.Foreground = Ui.Brush(Ui.Red); message.Text = result.Error; return; }
                if (register) { message.Foreground = Ui.Brush(Ui.Lime); message.Text = "Cadastro enviado. Aguarde a aprovação no app de gestão."; return; }
                Api.Token = Ui.Get(result.Data, "token"); Dictionary<string, object> user = result.Data.ContainsKey("user") ? result.Data["user"] as Dictionary<string, object> : null;
                if (string.Equals(Ui.Get(user, "forcePasswordReset"), "True", StringComparison.OrdinalIgnoreCase))
                {
                    ChangePasswordWindow change = new ChangePasswordWindow { Owner = this };
                    if (change.ShowDialog() != true) { Api.Token = null; message.Text = "Troque a senha temporária para continuar."; return; }
                    ApiResult changed = await Api.Post("/auth/change-password", new { password = change.Password }, null); Api.Token = null;
                    if (!changed.Ok) { message.Text = changed.Error; return; }
                    message.Foreground = Ui.Brush(Ui.Lime); message.Text = "Senha alterada. Entre novamente com sua nova senha."; return;
                }
                ClientMainWindow main = new ClientMainWindow(user); Application.Current.MainWindow = main; main.Show(); Close();
            };
            formHost.Children.Add(s); Ui.Enter(s);
        }
    }

    sealed class ClientTweak
    {
        public string Title, Description, Category, Impact, Risk, Path, Name, ButtonText, Confirmation; public RegistryHive Hive; public object Value; public RegistryValueKind Kind; public Action CustomApply; public bool Recommended;
    }
    sealed class ActionCancelledException : Exception { }

    sealed class ClientMainWindow : Window
    {
        readonly Grid page = new Grid(); readonly Dictionary<string, object> user; readonly List<ClientTweak> tweaks; readonly HashSet<string> access = new HashSet<string>(StringComparer.OrdinalIgnoreCase); readonly TextBlock title, subtitle;
        public ClientMainWindow(Dictionary<string, object> profile)
        {
            user = profile ?? new Dictionary<string, object>(); tweaks = BuildTweaks(); Ui.ConfigureWindow(this, "Marcão Boost", 1320, 820);
            Grid root = new Grid(); root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(230) }); root.ColumnDefinitions.Add(new ColumnDefinition()); Content = Ui.Chrome(this, root);
            Border nav = new Border { Background = Ui.Brush(Ui.Nav), Padding = new Thickness(20, 24, 20, 20) }; root.Children.Add(nav); StackPanel menu = new StackPanel(); nav.Child = menu;
            StackPanel brand = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4, 0, 0, 28) }; brand.Children.Add(Ui.BrandImage(54)); TextBlock brandText = Ui.TextBlock("MARCÃO\nBOOST", 17, Ui.Lime, FontWeights.Black); brandText.Margin = new Thickness(12, 0, 0, 0); brand.Children.Add(brandText); menu.Children.Add(brand);
            ArrayList rawPermissions = user.ContainsKey("permissions") ? user["permissions"] as ArrayList : null; if (rawPermissions != null) foreach (object p in rawPermissions) access.Add(Convert.ToString(p));
            AddNav(menu, "⌂   Painel", delegate { ShowDashboard(); }); if (access.Contains("optimize")) AddNav(menu, "⚡   Otimizações", delegate { ShowOptimizations("Todos"); }); if (access.Contains("cleanup")) AddNav(menu, "♲   Limpeza", delegate { ShowCleanup(); }); if (access.Contains("restore")) AddNav(menu, "↶   Restauração", delegate { ShowRestore(); }); AddNav(menu, "●   Minha conta", delegate { ShowAccount(); });
            Grid work = new Grid { Margin = new Thickness(34, 24, 34, 24) }; work.RowDefinitions.Add(new RowDefinition { Height = new GridLength(86) }); work.RowDefinitions.Add(new RowDefinition()); Grid.SetColumn(work, 1); root.Children.Add(work);
            StackPanel head = new StackPanel(); title = Ui.TextBlock("Painel", 30, Ui.Text, FontWeights.SemiBold); subtitle = Ui.TextBlock("Visão geral do seu PC", 14, Ui.Muted, FontWeights.Normal); subtitle.Margin = new Thickness(1, 4, 0, 0); head.Children.Add(title); head.Children.Add(subtitle); work.Children.Add(head); Grid.SetRow(page, 1); work.Children.Add(page); ShowDashboard();
        }
        void AddNav(Panel panel, string text, Action action) { Button b = Ui.Button(text, false, 190); b.HorizontalContentAlignment = HorizontalAlignment.Left; b.Margin = new Thickness(0, 0, 0, 8); b.Click += delegate { action(); }; panel.Children.Add(b); }
        void Swap(string heading, string sub, UIElement content) { title.Text = heading; subtitle.Text = sub; page.Children.Clear(); page.Children.Add(content); Ui.Enter(content); }
        void ShowDashboard()
        {
            WrapPanel flow = new WrapPanel(); flow.Children.Add(Metric("STATUS", "Pronto para acelerar", "Nenhuma mudança ocorre sem sua confirmação.", Ui.Lime)); flow.Children.Add(Metric("AJUSTES", tweaks.Count.ToString(), "Opções liberadas para desempenho, jogos e rede.", Ui.Blue)); flow.Children.Add(Metric("CONTA", Ui.Get(user, "name"), "Conectada à gestão Marcão Boost.", Color.FromRgb(185, 122, 255)));
            StackPanel recommend = new StackPanel(); recommend.Children.Add(Ui.TextBlock("Otimização recomendada", 21, Ui.Text, FontWeights.SemiBold)); TextBlock info = Ui.TextBlock(access.Contains("optimize") ? "Aplica somente ajustes de baixo risco. Mudanças persistentes recebem backup automático." : "Peça ao administrador para liberar as otimizações desta conta.", 14, Ui.Muted, FontWeights.Normal); info.Margin = new Thickness(0, 9, 0, 20); recommend.Children.Add(info); Button run = Ui.Button(access.Contains("optimize") ? "Executar recomendada" : "Acesso não liberado", true, 210); run.IsEnabled = access.Contains("optimize"); run.Click += delegate { RunRecommended(run); }; recommend.Children.Add(run); flow.Children.Add(Ui.Card(recommend, new Thickness(0, 14, 14, 0)));
            ScrollViewer scroll = Ui.InvisibleScroll(flow); Swap("Painel", "Um resumo rápido antes de otimizar", scroll);
        }
        Border Metric(string kicker, string value, string desc, Color accent) { StackPanel s = new StackPanel(); s.Children.Add(Ui.TextBlock(kicker, 12, accent, FontWeights.Bold)); TextBlock v = Ui.TextBlock(value, 21, Ui.Text, FontWeights.SemiBold); v.Margin = new Thickness(0, 14, 0, 10); s.Children.Add(v); s.Children.Add(Ui.TextBlock(desc, 13, Ui.Muted, FontWeights.Normal)); Border b = Ui.Card(s, new Thickness(0, 0, 14, 14)); b.Width = 290; b.Height = 166; return b; }
        void ShowOptimizations(string category)
        {
            DockPanel root = new DockPanel(); WrapPanel filters = new WrapPanel { Margin = new Thickness(0, 0, 0, 14) }; DockPanel.SetDock(filters, Dock.Top);
            foreach (string c in new[] { "Todos", "Games", "Desempenho", "Rede", "Aparência", "Privacidade", "NVIDIA", "AMD", "Hardware" }) { string selected = c; Button b = Ui.Button(c, c == category, Math.Max(84, c.Length * 9 + 28)); b.Height = 34; b.Margin = new Thickness(0, 0, 8, 8); b.Click += delegate { ShowOptimizations(selected); }; filters.Children.Add(b); } root.Children.Add(filters);
            WrapPanel cards = new WrapPanel(); foreach (ClientTweak t in tweaks.Where(x => category == "Todos" || x.Category == category)) cards.Children.Add(TweakCard(t)); ScrollViewer scroll = Ui.InvisibleScroll(cards); root.Children.Add(scroll); Swap("Otimizações", "Escolha só o que faz sentido para o seu uso", root);
        }
        Border TweakCard(ClientTweak t)
        {
            StackPanel s = new StackPanel(); s.Children.Add(Ui.TextBlock(t.Category.ToUpper() + "  •  " + t.Impact, 11, t.Risk == "Baixo risco" ? Ui.Lime : Ui.Amber, FontWeights.Bold)); TextBlock h = Ui.TextBlock(t.Title, 16, Ui.Text, FontWeights.SemiBold); h.Margin = new Thickness(0, 13, 0, 8); s.Children.Add(h); s.Children.Add(Ui.TextBlock(t.Description, 13, Ui.Muted, FontWeights.Normal));
            StackPanel actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 18, 0, 0) }; Button details = Ui.Button("Detalhes", false, 100); Button apply = Ui.Button(string.IsNullOrEmpty(t.ButtonText)?"Aplicar":t.ButtonText, true, string.IsNullOrEmpty(t.ButtonText)?110:135); apply.Margin = new Thickness(12, 0, 0, 0); details.Click += delegate { MessageBox.Show(t.Description + "\n\nImpacto esperado: " + t.Impact + "\nSegurança: " + t.Risk, t.Title); }; apply.Click += delegate { Apply(t, apply); }; actions.Children.Add(details); actions.Children.Add(apply); s.Children.Add(actions); Border card = Ui.Card(s, new Thickness(0, 0, 14, 14)); card.Width = 360; card.MinHeight = 205; return card;
        }
        void Apply(ClientTweak t, Button button)
        {
            string confirmation=string.IsNullOrEmpty(t.Confirmation)?t.Description+"\n\nUm backup será criado. Deseja aplicar?":t.Confirmation;if(MessageBox.Show(confirmation,t.Title,MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
            try { if (t.CustomApply != null) t.CustomApply(); else BackupAndSet(t); button.Content = "Aplicado ✓"; button.IsEnabled = false; Ui.SuccessSound(); } catch (ActionCancelledException) { return; } catch (Exception ex) { Ui.ErrorSound(); MessageBox.Show("Algo deu errado e a ação não foi concluída.\n\n" + ex.Message, "Marcão Boost", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }
        void RunRecommended(Button button)
        {
            List<ClientTweak> safe = tweaks.Where(x => x.Risk == "Baixo risco" && x.Recommended).ToList();
            if (MessageBox.Show("Aplicar " + safe.Count + " ajustes de baixo risco?\n\nAs configurações persistentes terão backup local.", "Otimização recomendada", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            int applied = 0; List<string> failed = new List<string>(); button.IsEnabled = false; button.Content = "Aplicando...";
            foreach (ClientTweak tweak in safe) try { if (tweak.CustomApply != null) tweak.CustomApply(); else BackupAndSet(tweak); applied++; } catch { failed.Add(tweak.Title); }
            button.Content = failed.Count == 0 ? "Concluído ✓" : applied + " aplicados"; if (failed.Count == 0) Ui.SuccessSound(); else Ui.ErrorSound();
            MessageBox.Show(applied + " ajustes aplicados." + (failed.Count == 0 ? "" : "\n\nNão concluídos: " + string.Join(", ", failed)), "Marcão Boost", MessageBoxButton.OK, failed.Count == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
        static void BackupAndSet(ClientTweak t)
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MarcaoBoost", "backups"); Directory.CreateDirectory(dir); string file = Path.Combine(dir, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff") + "-" + Guid.NewGuid().ToString("N").Substring(0,6) + ".mbak"); RegistryKey baseKey = t.Hive == RegistryHive.CurrentUser ? Registry.CurrentUser : Registry.LocalMachine;
            using (RegistryKey old = baseKey.OpenSubKey(t.Path)) { object v = old == null ? null : old.GetValue(t.Name, null, RegistryValueOptions.DoNotExpandEnvironmentNames); File.WriteAllText(file, t.Hive + "|" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(t.Path)) + "|" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(t.Name)) + "|" + (v == null ? "-1" : ((int)old.GetValueKind(t.Name)).ToString()) + "|" + (v == null ? "" : Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(Convert.ToString(v))))); }
            using (RegistryKey key = baseKey.CreateSubKey(t.Path)) key.SetValue(t.Name, t.Value, t.Kind);
        }
        void ShowCleanup()
        {
            StackPanel s = new StackPanel(); s.Children.Add(Ui.TextBlock("Limpeza profunda e segura", 22, Ui.Text, FontWeights.SemiBold)); TextBlock d = Ui.TextBlock("Verifica %TEMP%, Temp do Windows, Prefetch antigo, miniaturas, relatórios de erro e caches gráficos. Documentos pessoais não são tocados e arquivos em uso são ignorados.", 14, Ui.Muted, FontWeights.Normal); d.Margin = new Thickness(0, 8, 0, 22); s.Children.Add(d); TextBlock result = Ui.TextBlock("Clique em Analisar para localizar arquivos descartáveis.", 16, Ui.Blue, FontWeights.SemiBold); s.Children.Add(result); StackPanel a = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 22, 0, 0) }; Button scan = Ui.Button("Analisar", false, 130), clean = Ui.Button("Limpar agora", true, 150); clean.Margin = new Thickness(12, 0, 0, 0); clean.IsEnabled = false;
            scan.Click += async delegate { scan.IsEnabled=false;clean.IsEnabled=false;result.Text="Analisando locais seguros...";CleanupResult r=await Task.Run(delegate{return ScanJunk(false);});result.Text=r.Count+" arquivos • "+Format(r.Bytes)+" encontrados em "+r.Locations+" áreas";scan.IsEnabled=true;clean.IsEnabled=r.Count>0; };
            clean.Click += async delegate { if (MessageBox.Show("Remover os arquivos descartáveis encontrados?\n\nCaches necessários serão recriados automaticamente pelo Windows.", "Limpeza", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return; scan.IsEnabled=false;clean.IsEnabled=false;result.Text="Limpando...";CleanupResult r=await Task.Run(delegate{return ScanJunk(true);});scan.IsEnabled=true;result.Text="Limpeza concluída • "+Format(r.Bytes)+" liberados"+(r.Failed>0?" • "+r.Failed+" em uso/sem acesso":"");if(r.Failed==0)Ui.SuccessSound();else{Ui.ErrorSound();MessageBox.Show("A limpeza foi concluída, mas "+r.Failed+" arquivos não puderam ser removidos porque estavam em uso ou protegidos pelo Windows.","Limpeza parcial",MessageBoxButton.OK,MessageBoxImage.Warning);} };
            a.Children.Add(scan); a.Children.Add(clean); s.Children.Add(a); Border card = Ui.Card(s, new Thickness(0)); card.MaxWidth = 820; card.HorizontalAlignment = HorizontalAlignment.Left; Swap("Limpeza", "Libere espaço sem tocar nos seus documentos", card);
        }
        sealed class CleanupLocation { public string Path, Pattern; public int MinAgeHours; public bool Recursive; public CleanupLocation(string path,string pattern,int age,bool recursive){Path=path;Pattern=pattern;MinAgeHours=age;Recursive=recursive;} }
        sealed class CleanupResult { public int Count, Failed, Locations; public long Bytes; }
        static CleanupResult ScanJunk(bool delete)
        {
            string local=Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),windows=Environment.GetEnvironmentVariable("WINDIR")??@"C:\Windows",programData=Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            List<CleanupLocation> locations=new List<CleanupLocation>{
                new CleanupLocation(Path.GetTempPath(),"*",24,true),new CleanupLocation(Path.Combine(windows,"Temp"),"*",24,true),new CleanupLocation(Path.Combine(windows,"Prefetch"),"*",72,false),
                new CleanupLocation(Path.Combine(local,"D3DSCache"),"*",24,true),new CleanupLocation(Path.Combine(local,@"Microsoft\Windows\INetCache"),"*",24,true),new CleanupLocation(Path.Combine(local,@"Microsoft\Windows\Explorer"),"thumbcache_*.db",24,false),
                new CleanupLocation(Path.Combine(local,@"Microsoft\Windows\WER\ReportArchive"),"*",24,true),new CleanupLocation(Path.Combine(local,@"Microsoft\Windows\WER\ReportQueue"),"*",24,true),
                new CleanupLocation(Path.Combine(local,@"NVIDIA\DXCache"),"*",24,true),new CleanupLocation(Path.Combine(local,@"NVIDIA\GLCache"),"*",24,true),new CleanupLocation(Path.Combine(programData,@"NVIDIA Corporation\NV_Cache"),"*",24,true)
            };
            CleanupResult result=new CleanupResult();HashSet<string> seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(CleanupLocation location in locations){if(!Directory.Exists(location.Path))continue;result.Locations++;foreach(string file in EnumerateSafe(location.Path,location.Pattern,location.Recursive)){if(!seen.Add(file))continue;try{FileInfo info=new FileInfo(file);if(info.LastWriteTimeUtc>DateTime.UtcNow.AddHours(-location.MinAgeHours))continue;if(delete){long length=info.Length;try{info.IsReadOnly=false;info.Delete();result.Bytes+=length;result.Count++;}catch{result.Failed++;}}else{result.Bytes+=info.Length;result.Count++;}}catch{if(delete)result.Failed++;}}}return result;
        }
        static List<string> EnumerateSafe(string root,string pattern,bool recursive){List<string> files=new List<string>(),pending=new List<string>{root};for(int i=0;i<pending.Count;i++){string dir=pending[i];try{files.AddRange(Directory.GetFiles(dir,pattern,SearchOption.TopDirectoryOnly));}catch{}if(!recursive)continue;try{foreach(string child in Directory.GetDirectories(dir)){try{if((new DirectoryInfo(child).Attributes&FileAttributes.ReparsePoint)==0)pending.Add(child);}catch{}}}catch{}}return files;}
        static string Format(long b) { double n = b; string[] u = { "B", "KB", "MB", "GB" }; int i = 0; while (n >= 1024 && i < 3) { n /= 1024; i++; } return n.ToString(i == 0 ? "0" : "0.0") + " " + u[i]; }
        void ShowRestore() { StackPanel s = new StackPanel(); s.Children.Add(Ui.TextBlock("Backups automáticos", 22, Ui.Text, FontWeights.SemiBold)); string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MarcaoBoost", "backups"); string[] files = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.mbak").OrderByDescending(x => x).Take(25).ToArray() : new string[0]; TextBlock d = Ui.TextBlock(files.Length == 0 ? "Nenhum backup foi criado ainda." : files.Length + " backups disponíveis na pasta local.", 14, Ui.Muted, FontWeights.Normal); d.Margin = new Thickness(0, 10, 0, 20); s.Children.Add(d); StackPanel actions=new StackPanel{Orientation=Orientation.Horizontal};if(files.Length>0){Button restore=Ui.Button("Restaurar o mais recente",true,205);restore.Click+=delegate{if(MessageBox.Show("Restaurar o backup mais recente?","Restauração",MessageBoxButton.YesNo)!=MessageBoxResult.Yes)return;try{RestoreFile(files[0]);restore.Content="Restaurado ✓";restore.IsEnabled=false;Ui.SuccessSound();}catch(Exception ex){Ui.ErrorSound();MessageBox.Show("Algo deu errado durante a restauração.\n\n"+ex.Message,"Marcão Boost",MessageBoxButton.OK,MessageBoxImage.Warning);}};actions.Children.Add(restore);}Button open = Ui.Button("Abrir pasta de backups", false, 200); open.Margin=new Thickness(files.Length>0?12:0,0,0,0); open.Click += delegate { Directory.CreateDirectory(dir); Process.Start("explorer.exe", dir); };actions.Children.Add(open);s.Children.Add(actions); Swap("Restauração", "Volte às configurações anteriores quando quiser", Ui.Card(s, new Thickness(0))); }
        static void RestoreFile(string file){string[] lines=File.ReadAllLines(file);foreach(string line in lines.Reverse()){if(string.IsNullOrWhiteSpace(line)||line.StartsWith("MARCAO")||line.StartsWith("DATE|"))continue;string[] p=line.Split('|');if(p[0]=="GPU"&&p.Length>=4){Run(GpuHelper(p[1]),"restore "+p[2]+" "+p[3]);continue;}if(p[0]=="CMD"&&p.Length>1){string cmd=System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(p[1]));int split=cmd.IndexOf(' ');Run(split<0?cmd:cmd.Substring(0,split),split<0?"":cmd.Substring(split+1));continue;}int offset=p[0]=="REG"?1:0;if(p.Length<offset+5)continue;RegistryHive hive=(RegistryHive)Enum.Parse(typeof(RegistryHive),p[offset]);string path=System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(p[offset+1])),name=System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(p[offset+2]));int kind=int.Parse(p[offset+3]);RegistryKey baseKey=hive==RegistryHive.CurrentUser?Registry.CurrentUser:Registry.LocalMachine;using(RegistryKey key=baseKey.CreateSubKey(path)){if(kind<0)key.DeleteValue(name,false);else{string value=System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(p[offset+4]));RegistryValueKind k=(RegistryValueKind)kind;object typed=k==RegistryValueKind.DWord?(object)int.Parse(value):k==RegistryValueKind.QWord?(object)long.Parse(value):value;key.SetValue(name,typed,k);}}}}
        static string Run(string exe,string args){ProcessStartInfo psi=new ProcessStartInfo(exe,args){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};using(Process p=Process.Start(psi)){string output=p.StandardOutput.ReadToEnd();string error=p.StandardError.ReadToEnd();p.WaitForExit();if(p.ExitCode!=0)throw new InvalidOperationException(error);return output;}}
        void ShowAccount() { StackPanel s = new StackPanel(); s.Children.Add(Ui.TextBlock(Ui.Get(user, "name"), 23, Ui.Text, FontWeights.SemiBold)); TextBlock e = Ui.TextBlock(Ui.Get(user, "email"), 14, Ui.Muted, FontWeights.Normal); e.Margin = new Thickness(0, 8, 0, 20); s.Children.Add(e); s.Children.Add(Ui.TextBlock("Sua conta e permissões são administradas pelo Marcão Boost Gestão.", 14, Ui.Muted, FontWeights.Normal)); Button logout=Ui.Button("Sair da conta",false,150);logout.Margin=new Thickness(0,22,0,0);logout.Click+=delegate{Api.Token=null;ClientLoginWindow login=new ClientLoginWindow();Application.Current.MainWindow=login;login.Show();Close();};s.Children.Add(logout); Swap("Minha conta", "Acesso conectado e protegido", Ui.Card(s, new Thickness(0))); }
        static List<ClientTweak> BuildTweaks()
        {
            List<ClientTweak> list = new List<ClientTweak> {
                T("Ativar Modo de Jogo","Prioriza recursos durante partidas.","Games","Mais consistência","Baixo risco",RegistryHive.CurrentUser,@"Software\Microsoft\GameBar","AutoGameModeEnabled",1,RegistryValueKind.DWord),
                T("Desativar gravação em segundo plano","Impede a captura contínua do Xbox Game Bar.","Games","Menos GPU e disco","Baixo risco",RegistryHive.CurrentUser,@"System\GameConfigStore","GameDVR_Enabled",0,RegistryValueKind.DWord),
                T("Agendamento de GPU por hardware","Ativa HAGS em placas compatíveis; requer reiniciar.","Games","Pode reduzir latência","Atenção",RegistryHive.LocalMachine,@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers","HwSchMode",2,RegistryValueKind.DWord),
                T("Reduzir animações","Usa o perfil visual de melhor desempenho.","Desempenho","Interface mais ágil","Baixo risco",RegistryHive.CurrentUser,@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects","VisualFXSetting",2,RegistryValueKind.DWord),
                T("Remover atraso de inicialização","Retira a espera artificial antes dos programas iniciais.","Desempenho","Inicialização direta","Baixo risco",RegistryHive.CurrentUser,@"Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize","StartupDelayInMSec",0,RegistryValueKind.DWord),
                T("Acelerar menus","Reduz moderadamente o tempo de abertura dos menus.","Desempenho","Resposta mais rápida","Baixo risco",RegistryHive.CurrentUser,@"Control Panel\Desktop","MenuShowDelay","120",RegistryValueKind.String),
                T("Sensor de Armazenamento","Ativa a limpeza automática nativa do Windows.","Desempenho","Disco organizado","Baixo risco",RegistryHive.CurrentUser,@"Software\Microsoft\Windows\CurrentVersion\StorageSense\Parameters\StoragePolicy","01",1,RegistryValueKind.DWord),
                T("Remover limite multimídia","Evita limitação de rede durante cargas multimídia.","Rede","Pode reduzir latência","Atenção",RegistryHive.LocalMachine,@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile","NetworkThrottlingIndex",unchecked((int)0xffffffff),RegistryValueKind.DWord),
                T("Desativar compartilhamento de atualizações","Impede envio de atualizações a outros PCs.","Rede","Menos tráfego","Baixo risco",RegistryHive.LocalMachine,@"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization","DODownloadMode",0,RegistryValueKind.DWord),
                T("Desativar transparência","Remove efeitos de transparência da interface.","Aparência","Menos composição gráfica","Baixo risco",RegistryHive.CurrentUser,@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize","EnableTransparency",0,RegistryValueKind.DWord),
                T("Ocultar Widgets","Remove o botão Widgets da barra de tarefas.","Aparência","Barra mais limpa","Baixo risco",RegistryHive.CurrentUser,@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced","TaskbarDa",0,RegistryValueKind.DWord),
                T("Desativar dicas","Reduz sugestões promocionais do Windows.","Privacidade","Menos interrupções","Baixo risco",RegistryHive.CurrentUser,@"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager","SoftLandingEnabled",0,RegistryValueKind.DWord),
                T("Desativar ID de publicidade","Desliga o identificador de anúncios dos aplicativos.","Privacidade","Mais privacidade","Baixo risco",RegistryHive.CurrentUser,@"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo","Enabled",0,RegistryValueKind.DWord),
                T("Desativar sugestões web","Mantém a busca do menu Iniciar focada no conteúdo local.","Privacidade","Busca mais limpa","Baixo risco",RegistryHive.CurrentUser,@"Software\Policies\Microsoft\Windows\Explorer","DisableSearchBoxSuggestions",1,RegistryValueKind.DWord)
            };
            list.Add(TCustom("Limpar cache DNS","Remove endereços antigos do cache de rede sem trocar o servidor DNS.","Rede","Corrige cache antigo","Baixo risco",delegate{Run("ipconfig.exe","/flushdns");}));
            list.Add(TCustom("Plano Alto Desempenho","Prioriza resposta quando o computador está ligado; pode elevar consumo e temperatura.","Desempenho","Resposta consistente","Atenção",delegate{string current=Run("powercfg.exe","/getactivescheme");System.Text.RegularExpressions.Match m=System.Text.RegularExpressions.Regex.Match(current,@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MarcaoBoost","backups");Directory.CreateDirectory(dir);if(m.Success)File.WriteAllText(Path.Combine(dir,DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff")+".mbak"),"CMD|"+Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("powercfg.exe /setactive "+m.Value)));Run("powercfg.exe","/setactive SCHEME_MIN");}));
            list.Add(TCustom("Desativar aceleração do mouse","Deixa o movimento físico do mouse mais previsível para jogos competitivos.","Games","Mira consistente","Atenção",delegate{foreach(string n in new[]{"MouseSpeed","MouseThreshold1","MouseThreshold2"}){ClientTweak mt=T("","","","","",RegistryHive.CurrentUser,@"Control Panel\Mouse",n,"0",RegistryValueKind.String);BackupAndSet(mt);}}));
            list.Add(A("Atualizar driver da GPU","Abre as atualizações opcionais do Windows para procurar drivers estáveis. Para instalação limpa, utilize o instalador oficial do fabricante.","Hardware","Compatibilidade e desempenho","Revisão manual",delegate{Open("ms-settings:windowsupdate-optionalupdates");},"Procurar driver"));
            list.Add(A("Atualizar driver do chipset","Abre as atualizações opcionais. Drivers de chipset também podem ser obtidos no site oficial da fabricante da placa-mãe ou CPU.","Hardware","Energia e comunicação","Revisão manual",delegate{Open("ms-settings:windowsupdate-optionalupdates");},"Procurar driver"));
            list.Add(A("Revisar inicialização do Windows","Abre a lista oficial de aplicativos iniciados com o sistema para você desativar apenas o que não usa.","Desempenho","Menos programas residentes","Baixo risco",delegate{Open("ms-settings:startupapps");},"Abrir lista"));
            list.Add(A("Revisar programas antes de jogar","Abre o Gerenciador de Tarefas para identificar navegador, launchers, RGB, overlays e monitores que estão consumindo CPU, GPU ou RAM.","Games","Mais recursos livres","Revisão manual",delegate{Process.Start("taskmgr.exe");},"Abrir tarefas"));
            list.Add(A("Configurações gráficas e HAGS","Abre a área do Windows onde é possível testar o agendamento de GPU e definir a GPU de alto desempenho por jogo.","Games","Teste por computador","Revisão manual",delegate{Open("ms-settings:display-advancedgraphics");},"Abrir ajustes"));
            list.Add(A("Efeitos visuais avançados","Abre o painel clássico para escolher entre aparência e melhor desempenho.","Aparência","Menos efeitos visuais","Revisão manual",delegate{Process.Start("SystemPropertiesPerformance.exe");},"Abrir painel"));
            list.Add(A("Indexação do Windows Search","Abre as opções de indexação para limitar locais sem desativar o serviço inteiro.","Desempenho","Menos disco em segundo plano","Revisão manual",delegate{Run("control.exe","/name Microsoft.IndexingOptions");},"Revisar"));
            list.Add(A("Serviços opcionais","Abre a lista de serviços. Fax e Spooler só devem ser alterados quando você tiver certeza de que não usa esses recursos.","Desempenho","Controle de processos","Cuidado",delegate{Process.Start("services.msc");},"Revisar"));
            list.Add(A("Aplicativos instalados","Permite remover OneDrive, Copilot ou outros componentes apenas se você realmente não os utiliza.","Privacidade","Menos aplicativos","Revisão manual",delegate{Open("ms-settings:appsfeatures");},"Abrir apps"));
            list.Add(A("Verificar gargalo CPU/GPU","Abre a guia de desempenho. GPU próxima de 100% sugere limite gráfico; um núcleo de CPU saturado pode indicar limite de processador.","Hardware","Diagnóstico correto","Informativo",delegate{Process.Start("taskmgr.exe");},"Monitorar"));
            list.Add(A("Dual-channel e memória RAM","Abre as informações do sistema. A quantidade de canais e a frequência efetiva podem exigir verificação no Gerenciador de Tarefas ou BIOS.","Hardware","Potencial de FPS","Informativo",delegate{Process.Start("msinfo32.exe");},"Verificar"));
            list.Add(A("XMP / EXPO","Mostra informações do hardware para ajudar na conferência. A ativação deve ser feita manualmente na BIOS e somente quando a memória e a placa-mãe suportarem.","Hardware","Frequência correta da RAM","BIOS — cuidado",delegate{Process.Start("msinfo32.exe");},"Verificar"));
            list.Add(A("Resizable BAR / Smart Access Memory","Abre as informações do sistema. ReBAR, Above 4G Decoding e SAM dependem da GPU, placa-mãe, BIOS e driver.","Hardware","Pode ajudar em alguns jogos","BIOS — cuidado",delegate{Process.Start("msinfo32.exe");},"Verificar"));
            list.Add(A("Temperaturas e throttling","Abre o desempenho do Windows para uma verificação inicial de uso e clocks. Temperaturas exigem ferramenta de monitoramento compatível com seu hardware.","Hardware","Evita perda de clock","Informativo",delegate{Process.Start("taskmgr.exe");},"Monitorar"));
            list.Add(G("NVIDIA — desempenho máximo","Define o modo de energia global como Preferir desempenho máximo.","NVIDIA","Clock mais consistente","Atenção",delegate{GpuApply("nvidia","power","1");}));
            list.Add(G("NVIDIA — filtragem de textura","Define a qualidade da filtragem de textura como Alto desempenho.","NVIDIA","Possível ganho pequeno","Atenção",delegate{GpuApply("nvidia","texture","1");}));
            list.Add(G("NVIDIA — cache de shaders","Ativa o cache de shaders no perfil global do driver.","NVIDIA","Menos recompilação de shaders","Baixo risco",delegate{GpuApply("nvidia","shader","1");}));
            list.Add(G("NVIDIA — baixa latência","Ativa o modo de baixa latência quando o driver expõe essa opção pela NVAPI.","NVIDIA","Menor fila de renderização","Teste por jogo",delegate{GpuApply("nvidia","latency","1");}));
            list.Add(G("AMD — Radeon Anti-Lag","Ativa o Radeon Anti-Lag pela API oficial ADLX.","AMD","Pode reduzir latência","Teste por jogo",delegate{GpuApply("amd","antilag","1");}));
            list.Add(G("AMD — Radeon Chill","Desativa o Radeon Chill para evitar limitação dinâmica de FPS.","AMD","Evita limitação de FPS","Atenção",delegate{GpuApply("amd","chill","0");}));
            list.Add(G("AMD — Enhanced Sync","Desativa o Enhanced Sync para o preset de FPS máximo.","AMD","Sincronização previsível","Teste por jogo",delegate{GpuApply("amd","enhancedsync","0");}));
            list.Add(G("AMD — limite de quadros","Desativa o Frame Rate Target Control.","AMD","Remove limite configurado","Atenção",delegate{GpuApply("amd","frtc","0");}));
            list.Add(G("AMD — Radeon Boost","Ativa o Radeon Boost quando suportado; pode reduzir a resolução durante movimentos.","AMD","Mais FPS com redução visual","Atenção",delegate{GpuApply("amd","boost","1");}));
            list.Add(A("AMD — filtragem de textura","Abre o AMD Software para selecionar o perfil Performance quando desejado.","AMD","Possível ganho pequeno","Revisão manual",OpenAmd,"Abrir Adrenalin"));
            list.Add(A("AMD — GPU dedicada","Abre as configurações gráficas do Windows para escolher a GPU de alto desempenho para cada jogo.","AMD","Usa a placa correta","Baixo risco",delegate{Open("ms-settings:display-advancedgraphics");},"Escolher GPU"));
            return list;
        }
        static ClientTweak T(string title,string desc,string cat,string impact,string risk,RegistryHive hive,string path,string name,object value,RegistryValueKind kind) { return new ClientTweak { Title=title,Description=desc,Category=cat,Impact=impact,Risk=risk,Hive=hive,Path=path,Name=name,Value=value,Kind=kind,Recommended=true }; }
        static ClientTweak TCustom(string title,string desc,string cat,string impact,string risk,Action apply){return new ClientTweak{Title=title,Description=desc,Category=cat,Impact=impact,Risk=risk,CustomApply=apply,Recommended=true};}
        static ClientTweak A(string title,string desc,string cat,string impact,string risk,Action action,string button){return new ClientTweak{Title=title,Description=desc,Category=cat,Impact=impact,Risk=risk,CustomApply=action,ButtonText=button,Confirmation=desc+"\n\nDeseja abrir esta ferramenta agora?",Recommended=false};}
        static ClientTweak G(string title,string desc,string cat,string impact,string risk,Action action){return new ClientTweak{Title=title,Description=desc,Category=cat,Impact=impact,Risk=risk,CustomApply=action,ButtonText="Aplicar",Confirmation=desc+"\n\nO valor atual será salvo para restauração. Deseja aplicar?",Recommended=false};}
        static void GpuApply(string vendor,string key,string value){if(vendor=="amd"&&!AcceptAmd())throw new ActionCancelledException();string output=Run(GpuHelper(vendor),"set "+key+" "+value).Trim();if(!output.StartsWith("OK|"))throw new InvalidOperationException(output);string old=output.Substring(3);string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MarcaoBoost","backups");Directory.CreateDirectory(dir);File.WriteAllText(Path.Combine(dir,DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff")+"-gpu.mbak"),"GPU|"+vendor+"|"+key+"|"+old);}
        static string GpuHelper(string vendor){string name=vendor=="amd"?"MarcaoGpuAmd.exe":"MarcaoGpuNvidia.exe";string dir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MarcaoBoost","bin");Directory.CreateDirectory(dir);string path=Path.Combine(dir,name);using(Stream input=typeof(ClientEntry).Assembly.GetManifestResourceStream(name)){if(input==null)throw new InvalidOperationException("Componente de GPU não foi incorporado.");using(MemoryStream memory=new MemoryStream()){input.CopyTo(memory);byte[] data=memory.ToArray();if(!File.Exists(path)||new FileInfo(path).Length!=data.Length)File.WriteAllBytes(path,data);}}return path;}
        static bool AcceptAmd(){using(RegistryKey key=Registry.CurrentUser.CreateSubKey(@"Software\MarcaoBoost")){if(Convert.ToString(key.GetValue("AmdEulaAccepted"))=="1")return true;string terms="O recurso AMD destina-se somente a computadores com hardware AMD compatível. O software é fornecido como está, sem garantias. É proibido transferir, duplicar fora de backup razoável, desmontar, descompilar ou fazer engenharia reversa. O usuário deve cumprir leis de exportação aplicáveis. A AMD é terceira beneficiária destes termos e não responde por danos decorrentes do uso.";if(MessageBox.Show(terms+"\n\nVocê aceita os termos para usar a integração AMD ADLX?","Licença AMD ADLX",MessageBoxButton.YesNo,MessageBoxImage.Information)!=MessageBoxResult.Yes)return false;key.SetValue("AmdEulaAccepted","1",RegistryValueKind.String);return true;}}
        static void Open(string target){Process.Start(new ProcessStartInfo(target){UseShellExecute=true});}
        static void OpenNvidia(){string[] paths={Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),@"NVIDIA Corporation\Control Panel Client\nvcplui.exe"),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"nvcplui.exe")};string app=paths.FirstOrDefault(File.Exists);if(app==null)throw new InvalidOperationException("O Painel de Controle NVIDIA não foi encontrado. Instale ou atualize o driver NVIDIA.");Process.Start(app);}
        static void OpenAmd(){string app=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),@"AMD\CNext\CNext\RadeonSoftware.exe");if(!File.Exists(app))throw new InvalidOperationException("O AMD Software: Adrenalin Edition não foi encontrado. Instale ou atualize o driver AMD.");Process.Start(app);}
    }

    sealed class ChangePasswordWindow : Window
    {
        readonly PasswordBox password = new PasswordBox(), confirm = new PasswordBox(); public string Password { get { return password.Password; } }
        public ChangePasswordWindow()
        {
            Title = "Criar nova senha"; Width = 470; Height = 430; WindowStartupLocation = WindowStartupLocation.CenterOwner; Background = Ui.Brush(Ui.Bg); Foreground = Ui.Brush(Ui.Text); FontFamily = new FontFamily("Segoe UI"); Icon = Ui.WindowIcon();
            StackPanel s = new StackPanel { Margin = new Thickness(32) }; s.Children.Add(Ui.TextBlock("Troque a senha temporária", 23, Ui.Text, FontWeights.SemiBold)); TextBlock d = Ui.TextBlock("Somente você conhecerá a nova senha. Use pelo menos 10 caracteres.", 13, Ui.Muted, FontWeights.Normal); d.Margin = new Thickness(0, 8, 0, 20); s.Children.Add(d); s.Children.Add(Ui.Field(password, "NOVA SENHA")); s.Children.Add(Ui.Field(confirm, "CONFIRMAR SENHA")); Button save = Ui.Button("Salvar nova senha", true, 190); save.Click += delegate { if (password.Password.Length < 10) { Ui.ErrorSound(); MessageBox.Show("Use pelo menos 10 caracteres."); return; } if (password.Password != confirm.Password) { Ui.ErrorSound(); MessageBox.Show("As senhas não coincidem."); return; } DialogResult = true; }; s.Children.Add(save); Content = Ui.Chrome(this, s);
        }
    }
}
