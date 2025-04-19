using System.Collections.Generic;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
namespace AutoCompleteBoxTest.ViewModels;

public class MainWindowViewModel : ReactiveValidationObject {
    public string Greeting { get; } = "Try writing \"Hello world\" or \"Goodbye\"";

    public string Message {
        get => this._message;
        set => this.RaiseAndSetIfChanged(ref this._message, value);
    }
    private string _message = "";

    public List<string> Messages { get; } = new List<string> { "Hello world", "Goodbye", };


    public MainWindowViewModel() {
        this.ValidationRule(x => x.Message,
            message => 
                message != null && this.Messages.Contains(message),
            "Not a valid message, must be \"Hello world\" or \"Goodbye\"");
    }

    public void CmdClearMessage() {
        this.Message = "";
    }

}
