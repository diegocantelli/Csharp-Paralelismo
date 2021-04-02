using ByteBank.Core.Model;
using ByteBank.Core.Repository;
using ByteBank.Core.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ByteBank.View
{
    public partial class MainWindow : Window
    {
        private readonly ContaClienteRepository r_Repositorio;
        private readonly ContaClienteService r_Servico;

        public MainWindow()
        {
            InitializeComponent();

            r_Repositorio = new ContaClienteRepository();
            r_Servico = new ContaClienteService();
        }

        private void BtnProcessar_Click(object sender, RoutedEventArgs e)
        {
            // TaskScheduler.FromCurrentSynchronizationContext() -> retorna uma instância da thread que está sendo
            // executada no momento. No caso será a thread principal(UI)
            var taskSchedulerUI = TaskScheduler.FromCurrentSynchronizationContext();

            BtnProcessar.IsEnabled = false;

            var contas = r_Repositorio.GetContaClientes();

            var resultado = new List<string>();

            AtualizarView(new List<string>(), TimeSpan.Zero);

            var inicio = DateTime.Now;

            //Para cada conta na lista será criada uma nova Task
            var contasTarefas = contas.Select(conta =>
            {
                //Task.Factory.StartNew -> Abstrai a necessidade da criação de threads, a própria função
                // se ocupa de verificar o processamento e os núcleos disponíveis ou menos sobrecarregados para
                // executar o código
                return Task.Factory.StartNew(() =>
                {
                    //código que será executado na task
                    var resultadoConta = r_Servico.ConsolidarMovimentacao(conta);
                    resultado.Add(resultadoConta);
                });
                //To Array executa o código 
            }).ToArray();


            //Cria uma tarefa que será concluída quando todas as tarefas dentro do array forem finalizadas.
            // Ao contrário de WaitAll, WhenAll não bloqueia a thread principal, neste caso a thread da UI
            Task.WhenAll(contasTarefas)
                // O código dentro de ContinueWith só será executado quando a Task anterior for finalizada,
                // no caso todas as tarefas dentro de Task.WhenAll(contasTarefas)
                // task ->  Task.WhenAll(contasTarefas)
                .ContinueWith(task =>
                {
                    var fim = DateTime.Now;

                    //Atualizar a view só faz sentido quando todas as tasks forem finalizadas
                    AtualizarView(resultado, fim - inicio);
                    //taskSchedulerUI -> indica que o código dentro de ContinueWith deverá ser executado no
                    // contexto da Thread principal
                }, taskSchedulerUI)
                .ContinueWith(task =>
                {
                    BtnProcessar.IsEnabled = true;
                }, taskSchedulerUI);
            
           
        }

        private void AtualizarView(List<String> result, TimeSpan elapsedTime)
        {
            var tempoDecorrido = $"{ elapsedTime.Seconds }.{ elapsedTime.Milliseconds} segundos!";
            var mensagem = $"Processamento de {result.Count} clientes em {tempoDecorrido}";

            LstResultados.ItemsSource = result;
            TxtTempo.Text = mensagem;
        }
    }
}
