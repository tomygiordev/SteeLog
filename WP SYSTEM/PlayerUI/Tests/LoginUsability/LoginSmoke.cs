using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WP_Inventario.Servicios;
using WP_Inventario.Vistas;

internal static class LoginSmoke
{
    static int checks;
    static void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); Console.WriteLine("PASS " + message); }
    static T Field<T>(object o, string name) { return (T)o.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(o); }
    static object Prop(object o, string name) { return o.GetType().GetProperty(name).GetValue(o, null); }
    static Vista_Login Make(Func<string,string,Task<AuthResult>> auth)
    {
        var form = (Vista_Login)Activator.CreateInstance(typeof(Vista_Login), BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { auth }, null);
        form.ShowInTaskbar = false; form.StartPosition = FormStartPosition.Manual; form.Location = new Point(-20000, -20000); form.Show(); Application.DoEvents(); return form;
    }
    static void Pump(Func<bool> done)
    { for (int i=0; i<300 && !done(); i++) { Application.DoEvents(); Thread.Sleep(10); } Check(done(), "async completion"); }
    static double Lum(Color c) { Func<byte,double> lin = b => { double v = b / 255.0; return v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4); }; return 0.2126*lin(c.R)+0.7152*lin(c.G)+0.0722*lin(c.B); }
    static void Contrast(string name, Color a, Color b, double min) { double x=Lum(a), y=Lum(b); double value=(Math.Max(x,y)+0.05)/(Math.Min(x,y)+0.05); Console.WriteLine(name + " " + value.ToString("F2") + ":1"); Check(value>=min, name); }
    static bool Enter(Vista_Login form) { return (bool)typeof(Form).GetMethod("ProcessDialogKey", BindingFlags.Instance|BindingFlags.NonPublic).Invoke(form,new object[]{Keys.Enter}); }
    static void ClickHandler(Vista_Login form) { typeof(Vista_Login).GetMethod("BtnLogin_Click", BindingFlags.Instance|BindingFlags.NonPublic).Invoke(form,new object[]{null, EventArgs.Empty}); }
    static void FocusCapture(Control c, Vista_Login form, string path, Color focus)
    {
        c.Focus(); Application.DoEvents(); Check(c.Focused, "focus " + c.AccessibleName);
        using(var image = new Bitmap(form.Width,form.Height))
        { form.DrawToBitmap(image,new Rectangle(0,0,image.Width,image.Height)); image.Save(path); }
    }
    [STAThread] static void Main(string[] args)
    {
        string outDir=args[0]; Directory.CreateDirectory(outDir); int calls=0; string sentEmail=null, sentPassword=null;
        var tcs=new TaskCompletionSource<AuthResult>();
        Func<string,string,Task<AuthResult>> auth=(email,password)=> {calls++; sentEmail=email; sentPassword=password; return tcs.Task;};
        using(var form=Make(auth))
        {
            var email=Field<TextBox>(form,"txtEmail"); var password=Field<TextBox>(form,"txtPassword"); var login=Field<Control>(form,"btnLogin"); var card=Field<Panel>(form,"panelCard"); var error=Field<Label>(form,"lblError"); var version=Field<Label>(form,"lblVersion");
            Button close=null; foreach(Control c in form.Controls) if(c is Button) close=(Button)c;
            Check(close != null && email.AccessibleName=="Usuario" && password.AccessibleName=="Contraseña" && login.AccessibleName=="Ingresar" && close.AccessibleName=="Cerrar aplicación", "accessible names");
            Check(object.ReferenceEquals(form.AcceptButton,login), "accept button");
            email.Focus(); Application.DoEvents(); Check(form.SelectNextControl(email,true,true,true,true) && password.Focused,"Tab usuario → contraseña");
            Check(form.SelectNextControl(password,true,true,true,true) && login.Focused,"Tab contraseña → ingresar");
            Check(form.SelectNextControl(login,true,true,true,true) && close.Focused,"Tab ingresar → cerrar");
            var iconResource=typeof(Vista_Login).Assembly.GetType("WP_Inventario.Properties.Resources").GetProperty("SteeLogIcon",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null,null) as Icon;
            using(var a=new MemoryStream()) using(var b=new MemoryStream()) {form.Icon.Save(a); iconResource.Save(b); Check(System.Linq.Enumerable.SequenceEqual(a.ToArray(),b.ToArray()),"login icon");}
            var normal=(Color)Prop(login,"FillColor"); var hover=(Color)Prop(Prop(login,"HoverState"),"FillColor"); var disabled=Prop(login,"DisabledState");
            Contrast("button normal text",login.ForeColor,normal,4.5); Contrast("button hover text",login.ForeColor,hover,4.5); Contrast("button disabled text",(Color)Prop(disabled,"ForeColor"),(Color)Prop(disabled,"FillColor"),4.5);
            Contrast("button boundary",normal,card.BackColor,3); Contrast("button hover boundary",hover,card.BackColor,3);
            Contrast("version",version.ForeColor,card.BackColor,4.5); Contrast("error",error.ForeColor,card.BackColor,4.5);
            Contrast("close",close.ForeColor,form.BackColor,4.5); Contrast("close hover",close.ForeColor,close.FlatAppearance.MouseOverBackColor,4.5);
            var focus=(Color)typeof(Vista_Login).GetField("FocusColor",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
            Contrast("input border vs card",focus,card.BackColor,3); Contrast("input border vs fill",focus,email.BackColor,3); Contrast("focus vs card",Color.White,card.BackColor,3); Contrast("close focus vs form",focus,form.BackColor,3);
            FocusCapture(email,form,Path.Combine(outDir,"focus-usuario.png"),focus); FocusCapture(password,form,Path.Combine(outDir,"focus-password.png"),focus); FocusCapture(login,form,Path.Combine(outDir,"focus-login.png"),focus); FocusCapture(close,form,Path.Combine(outDir,"focus-close.png"),focus);
            email.Text="";password.Text=""; email.Focus(); Enter(form); Application.DoEvents(); Check(calls==0 && error.Visible,"empty fields do not call auth");
            email.Text=" admin ";password.Text="demo";email.Focus(); Check(Enter(form),"real Enter handled"); Check(calls==1 && sentEmail=="admin@tesis.local" && sentPassword=="demo","Enter calls normalized auth once"); Check(!login.Enabled && login.Text=="Verificando...","in-flight state");
            ClickHandler(form); Enter(form); Application.DoEvents(); Check(calls==1 && !form.IsDisposed && form.Visible,"single-flight and form stays open on repeated click and Enter");
            tcs.SetResult(new AuthResult {Success=false,ErrorMessage="Fallo demo"}); Pump(()=>login.Enabled); Check(error.Visible && error.Text=="Fallo demo" && login.Text=="Ingresar","failure restored and shown");
            tcs=new TaskCompletionSource<AuthResult>(); email.Text="operador"; Enter(form); Check(calls==2 && sentEmail=="galpon@tesis.local","Enter retry and alias"); tcs.SetException(new InvalidOperationException("demo")); Pump(()=>login.Enabled); Check(error.Visible && error.Text.Contains("demo"),"exception restored and shown");
        }
        SessionManager.Clear(); var pending=new TaskCompletionSource<AuthResult>(); int disposedCalls=0;
        var disposed=Make((u,p)=>{disposedCalls++;return pending.Task;}); Field<TextBox>(disposed,"txtEmail").Text="demo"; Field<TextBox>(disposed,"txtPassword").Text="demo"; ClickHandler(disposed); disposed.Close(); disposed.Dispose(); pending.SetResult(new AuthResult {Success=true,Email="unexpected@example.test"});
        for(int i=0;i<30;i++){Application.DoEvents();Thread.Sleep(5);} Check(disposedCalls==1 && disposed.IsDisposed && !SessionManager.IsLoggedIn,"disposed success has no session side effects");
        Console.WriteLine("OK " + checks + " assertions");
    }
}
