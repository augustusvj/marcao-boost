using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Reflection;

[assembly: AssemblyTitle("Marcão Boost Gestão")]
[assembly: AssemblyProduct("Marcão Boost Gestão")]
[assembly: AssemblyDescription("Gestão central de clientes e permissões")]
[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]

namespace MarcaoBoostWpf
{
    static class AdminEntry
    {
        [STAThread]
        public static void Main() { Application app = new Application(); app.ShutdownMode=ShutdownMode.OnLastWindowClose;AdminLoginWindow login=new AdminLoginWindow();app.MainWindow=login;app.Run(login); }
    }

    sealed class AdminLoginWindow : Window
    {
        readonly Grid host = new Grid(); bool bootstrap;
        public AdminLoginWindow()
        {
            Ui.ConfigureWindow(this,"Marcão Boost Gestão — Acesso",1040,670);
            Grid root = new Grid(); root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42, GridUnitType.Star) }); root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58, GridUnitType.Star) }); Content = Ui.Chrome(this, root);
            Border left = new Border { Background = Ui.Brush(Ui.Nav), Padding = new Thickness(48) }; StackPanel brand = new StackPanel { VerticalAlignment = VerticalAlignment.Center }; brand.Children.Add(Ui.BrandImage(138)); TextBlock n=Ui.TextBlock("MARCÃO BOOST\nGESTÃO",30,Ui.Lime,FontWeights.Black);n.TextAlignment=TextAlignment.Center;n.Margin=new Thickness(0,22,0,16);brand.Children.Add(n);TextBlock d=Ui.TextBlock("Controle central de clientes, acessos e permissões.",15,Ui.Muted,FontWeights.Normal);d.TextAlignment=TextAlignment.Center;brand.Children.Add(d);left.Child=brand;root.Children.Add(left);
            Border right = new Border { Background=Ui.Brush(Ui.Bg),Padding=new Thickness(76,45,76,45),Child=host };Grid.SetColumn(right,1);root.Children.Add(right); Show(false);
        }
        void Show(bool first)
        {
            bootstrap=first; host.Children.Clear(); StackPanel s=new StackPanel{VerticalAlignment=VerticalAlignment.Center,MaxWidth=410};s.Children.Add(Ui.TextBlock(first?"Criar proprietário":"Acesso administrativo",29,Ui.Text,FontWeights.SemiBold));TextBlock sub=Ui.TextBlock(first?"Use apenas uma vez para criar sua conta exclusiva de gestão.":"Entre com a conta de proprietário do Marcão Boost.",14,Ui.Muted,FontWeights.Normal);sub.Margin=new Thickness(0,8,0,26);s.Children.Add(sub);
            TextBox name=new TextBox();if(first)s.Children.Add(Ui.Field(name,"SEU NOME"));TextBox email=new TextBox();PasswordBox pass=new PasswordBox();s.Children.Add(Ui.Field(email,"E-MAIL"));s.Children.Add(Ui.Field(pass,first?"SENHA — MÍNIMO 10 CARACTERES":"SENHA"));TextBlock msg=Ui.TextBlock("",13,Ui.Red,FontWeights.Normal);msg.Margin=new Thickness(4,0,0,12);s.Children.Add(msg);
            Button go=Ui.Button(first?"Criar acesso proprietário":"Entrar na gestão",true,410);s.Children.Add(go);Button toggle=Ui.Button(first?"Voltar para o login":"Primeiro acesso",false,410);toggle.Margin=new Thickness(0,12,0,0);toggle.Click+=delegate{Show(!bootstrap);};s.Children.Add(toggle);
            go.Click+=async delegate { go.IsEnabled=false;msg.Text="Conectando...";ApiResult r= first?await Api.Post("/admin/bootstrap",new{name=name.Text,email=email.Text,password=pass.Password},Bootstrap.Secret):await Api.Post("/auth/login",new{email=email.Text,password=pass.Password},null);go.IsEnabled=true;if(!r.Ok){Ui.ErrorSound();msg.Text=r.Error;return;}if(first){Ui.SuccessSound();msg.Foreground=Ui.Brush(Ui.Lime);msg.Text="Proprietário criado. Agora entre com sua conta.";return;}Dictionary<string,object> user=r.Data.ContainsKey("user")?r.Data["user"] as Dictionary<string,object>:null;if(Ui.Get(user,"role")!="admin"){Ui.ErrorSound();msg.Text="Esta conta não possui acesso à gestão.";return;}Ui.SuccessSound();Api.Token=Ui.Get(r.Data,"token");AdminMainWindow main=new AdminMainWindow(user);Application.Current.MainWindow=main;main.Show();Close();};
            host.Children.Add(s);Ui.Enter(s);
        }
    }

    sealed class AdminMainWindow : Window
    {
        readonly StackPanel usersPanel=new StackPanel();readonly TextBlock summary;readonly TextBox search=new TextBox();readonly Dictionary<string,object> owner;
        public AdminMainWindow(Dictionary<string,object> profile)
        {
            owner=profile??new Dictionary<string,object>();Ui.ConfigureWindow(this,"Marcão Boost Gestão",1280,790);
            Grid root=new Grid();root.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(235)});root.ColumnDefinitions.Add(new ColumnDefinition());Content=Ui.Chrome(this,root);
            Border nav=new Border{Background=Ui.Brush(Ui.Nav),Padding=new Thickness(22,26,22,22)};StackPanel menu=new StackPanel();StackPanel brand=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(2,0,0,28)};brand.Children.Add(Ui.BrandImage(54));TextBlock bt=Ui.TextBlock("MARCÃO BOOST\nGESTÃO",16,Ui.Lime,FontWeights.Black);bt.Margin=new Thickness(12,0,0,0);brand.Children.Add(bt);menu.Children.Add(brand);Button clients=Ui.Button("●   Clientes",false,190);clients.HorizontalContentAlignment=HorizontalAlignment.Left;menu.Children.Add(clients);TextBlock signed=Ui.TextBlock("Conectado como\n"+Ui.Get(owner,"name"),12,Ui.Muted,FontWeights.Normal);signed.Margin=new Thickness(8,30,0,0);menu.Children.Add(signed);nav.Child=menu;root.Children.Add(nav);
            Grid work=new Grid{Margin=new Thickness(34,26,34,24)};work.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});work.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});work.RowDefinitions.Add(new RowDefinition());Grid.SetColumn(work,1);root.Children.Add(work);
            TextBlock title=Ui.TextBlock("Clientes",30,Ui.Text,FontWeights.SemiBold);work.Children.Add(title);summary=Ui.TextBlock("Carregando cadastros...",14,Ui.Muted,FontWeights.Normal);summary.Margin=new Thickness(0,44,0,20);work.Children.Add(summary);
            StackPanel tools=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,86,0,16)};Border sf=Ui.Field(search,"BUSCAR POR NOME OU E-MAIL");sf.Width=430;sf.Margin=new Thickness(0,0,12,0);tools.Children.Add(sf);Button find=Ui.Button("Buscar",false,110);find.Click+=async delegate{await LoadUsers();};tools.Children.Add(find);Button refresh=Ui.Button("Atualizar",true,120);refresh.Margin=new Thickness(10,0,0,0);refresh.Click+=async delegate{search.Text="";await LoadUsers();};tools.Children.Add(refresh);Grid.SetRow(tools,1);work.Children.Add(tools);
            ScrollViewer scroll=Ui.InvisibleScroll(usersPanel);Grid.SetRow(scroll,2);work.Children.Add(scroll);Loaded+=async delegate{await LoadUsers();};
        }
        async Task LoadUsers()
        {
            summary.Text="Atualizando...";ApiResult r=await Api.Get("/admin/users?q="+Uri.EscapeDataString(search.Text.Trim()));usersPanel.Children.Clear();if(!r.Ok){summary.Text=r.Error;return;}ArrayList rows=r.Data.ContainsKey("users")?r.Data["users"] as ArrayList:null;int total=rows==null?0:rows.Count;int pending=0;if(rows!=null)foreach(object item in rows){Dictionary<string,object> u=item as Dictionary<string,object>;if(Ui.Get(u,"status")=="pending")pending++;usersPanel.Children.Add(UserCard(u));}summary.Text=total+" clientes • "+pending+" aguardando aprovação";if(total==0)usersPanel.Children.Add(Ui.TextBlock("Nenhum cadastro encontrado.",15,Ui.Muted,FontWeights.Normal));Ui.Enter(usersPanel);
        }
        Border UserCard(Dictionary<string,object> u)
        {
            Grid g=new Grid();g.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});g.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(390)});
            StackPanel info=new StackPanel();info.Children.Add(Ui.TextBlock(Ui.Get(u,"name"),18,Ui.Text,FontWeights.SemiBold));TextBlock em=Ui.TextBlock(Ui.Get(u,"email"),13,Ui.Muted,FontWeights.Normal);em.Margin=new Thickness(0,5,0,8);info.Children.Add(em);string status=Ui.Get(u,"status");Color sc=status=="active"?Ui.Lime:status=="blocked"?Ui.Red:Ui.Amber;info.Children.Add(Ui.TextBlock(status=="active"?"● Ativo":status=="blocked"?"● Bloqueado":"● Aguardando aprovação",12,sc,FontWeights.Bold));g.Children.Add(info);
            StackPanel actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Center};Button state=Ui.Button(status=="active"?"Bloquear":"Ativar",status!="active",105);Button perms=Ui.Button("Permissões",false,112);Button reset=Ui.Button("Redefinir senha",false,140);perms.Margin=new Thickness(9,0,0,0);reset.Margin=new Thickness(9,0,0,0);string uid=Ui.Get(u,"id");state.Click+=async delegate{string next=status=="active"?"blocked":"active";if(MessageBox.Show("Alterar o acesso de "+Ui.Get(u,"name")+" para "+next+"?","Confirmar",MessageBoxButton.YesNo)!=MessageBoxResult.Yes)return;ApiResult r=await Api.Patch("/admin/users/"+uid+"/status",new{status=next});if(!r.Ok){Ui.ErrorSound();MessageBox.Show("Algo deu errado.\n\n"+r.Error,"Marcão Boost Gestão",MessageBoxButton.OK,MessageBoxImage.Warning);}else Ui.SuccessSound();await LoadUsers();};perms.Click+=async delegate{PermissionsDialog d=new PermissionsDialog(u){Owner=this};if(d.ShowDialog()==true){ApiResult r=await Api.Patch("/admin/users/"+uid+"/permissions",new{permissions=d.Selected});if(!r.Ok){Ui.ErrorSound();MessageBox.Show("Algo deu errado.\n\n"+r.Error,"Marcão Boost Gestão",MessageBoxButton.OK,MessageBoxImage.Warning);}else{Ui.SuccessSound();await LoadUsers();}}};reset.Click+=async delegate{ResetDialog d=new ResetDialog(Ui.Get(u,"name")){Owner=this};if(d.ShowDialog()==true){ApiResult r=await Api.Patch("/admin/users/"+uid+"/reset-password",new{temporaryPassword=d.Password});if(!r.Ok){Ui.ErrorSound();MessageBox.Show("Algo deu errado.\n\n"+r.Error,"Marcão Boost Gestão",MessageBoxButton.OK,MessageBoxImage.Warning);}else{Ui.SuccessSound();MessageBox.Show("Senha temporária definida. O cliente deverá trocá-la no próximo acesso.","Concluído");}}};actions.Children.Add(state);actions.Children.Add(perms);actions.Children.Add(reset);Grid.SetColumn(actions,1);g.Children.Add(actions);return Ui.Card(g,new Thickness(0,0,0,12));
        }
    }

    sealed class PermissionsDialog:Window
    {
        readonly CheckBox optimize=new CheckBox{Content="Otimizações"},cleanup=new CheckBox{Content="Limpeza"},restore=new CheckBox{Content="Restauração"};public string[] Selected{get{return new[]{optimize.IsChecked==true?"optimize":null,cleanup.IsChecked==true?"cleanup":null,restore.IsChecked==true?"restore":null}.Where(x=>x!=null).ToArray();}}
        public PermissionsDialog(Dictionary<string,object> user){Title="Permissões";Width=420;Height=430;WindowStartupLocation=WindowStartupLocation.CenterOwner;Background=Ui.Brush(Ui.Bg);Foreground=Ui.Brush(Ui.Text);FontFamily=new FontFamily("Segoe UI");Icon=Ui.WindowIcon();StackPanel s=new StackPanel{Margin=new Thickness(30)};s.Children.Add(Ui.TextBlock("Permissões do cliente",23,Ui.Text,FontWeights.SemiBold));TextBlock d=Ui.TextBlock("Escolha quais áreas ficarão disponíveis após o login.",13,Ui.Muted,FontWeights.Normal);d.Margin=new Thickness(0,8,0,20);s.Children.Add(d);HashSet<string> current=new HashSet<string>(StringComparer.OrdinalIgnoreCase);ArrayList values=user.ContainsKey("permissions")?user["permissions"] as ArrayList:null;if(values!=null)foreach(object v in values)current.Add(Convert.ToString(v));optimize.IsChecked=current.Contains("optimize");cleanup.IsChecked=current.Contains("cleanup");restore.IsChecked=current.Contains("restore");foreach(CheckBox c in new[]{optimize,cleanup,restore}){c.FontSize=15;c.Margin=new Thickness(0,8,0,8);c.Foreground=Ui.Brush(Ui.Text);s.Children.Add(c);}Button save=Ui.Button("Salvar permissões",true,190);save.Margin=new Thickness(0,20,0,0);save.Click+=delegate{DialogResult=true;};s.Children.Add(save);Content=Ui.Chrome(this,s);}
    }

    sealed class ResetDialog:Window
    {
        readonly TextBox password=new TextBox();public string Password{get{return password.Text;}}
        public ResetDialog(string name){Title="Redefinir senha";Width=480;Height=390;WindowStartupLocation=WindowStartupLocation.CenterOwner;Background=Ui.Brush(Ui.Bg);Foreground=Ui.Brush(Ui.Text);FontFamily=new FontFamily("Segoe UI");Icon=Ui.WindowIcon();StackPanel s=new StackPanel{Margin=new Thickness(30)};s.Children.Add(Ui.TextBlock("Senha temporária",23,Ui.Text,FontWeights.SemiBold));TextBlock d=Ui.TextBlock("Gere uma senha para "+name+". O cliente será obrigado a criar outra após entrar.",13,Ui.Muted,FontWeights.Normal);d.Margin=new Thickness(0,8,0,18);s.Children.Add(d);password.Text=Generate();s.Children.Add(Ui.Field(password,"SENHA TEMPORÁRIA"));Button done=Ui.Button("Confirmar redefinição",true,210);done.Click+=delegate{if(password.Text.Length<10){Ui.ErrorSound();MessageBox.Show("Use pelo menos 10 caracteres.");return;}DialogResult=true;};s.Children.Add(done);Content=Ui.Chrome(this,s);}
        static string Generate(){const string chars="ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#";byte[] b=new byte[14];RandomNumberGenerator.Create().GetBytes(b);char[] value=new char[b.Length];for(int i=0;i<b.Length;i++)value[i]=chars[b[i]%chars.Length];return new string(value);}
    }
}
