using Prism.Commands;
using Prism.Mvvm;
using System.Linq;
using System.Reflection;

namespace PL.Common.Prism
{
    /// <summary>Helper for commands.
    /// </summary>
    public static class CommandHelper
    {
        /// <summary>Invokes the ChangeCanExecute of all commands of the target VM.
        /// </summary>
        /// <param name="target">The target.</param>
        public static void InvokeChangeCanExecute(this BindableBase target)
        {
            if (target != null)
            {
                var commands = target.GetType()
                    // Only the public ICommand properties are of interest. The private's
                    // can never be visible in XAML. Moreover, the private's are probably
                    // the backing fields only.
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    // Get only the properties of type ICommand.
                    .Where(pi => pi.PropertyType == typeof(System.Windows.Input.ICommand))
                    // Determine the ChangeCanExecute method on that command. It depends
                    // on the implementation of the interface whether this method is available.
                    // RelayCommand and RelayCommand<T> both have it.
                    .Select(pi => pi.GetValue(target, null));
                foreach (var commandInterface in commands)
                {
                    var cmdObj = commandInterface as DelegateCommandBase;
                    if (cmdObj != null)
                    {
                        cmdObj.RaiseCanExecuteChanged();
                    }
                }
            }
        }
    }
}