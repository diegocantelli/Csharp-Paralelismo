using ByteBank.Core.Model;
using ByteBank.Core.Repository;
using ByteBank.Core.Service;
using ByteBank.View.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

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

        private async void BtnProcessar_Click(object sender, RoutedEventArgs e)
        {
            BtnProcessar.IsEnabled = false;

            var contas = r_Repositorio.GetContaClientes();

            PgsProgresso.Maximum = contas.Count();
            LimparView();

            var inicio = DateTime.Now;

            var byteBankProgress = new ByteBankProgress<string>(str => PgsProgresso.Value++);

            //Await -> Irá aguardar o resultado da task ConsolidarContas
            var resultado = await ConsolidarContas(contas, byteBankProgress);

            var fim = DateTime.Now;
            AtualizarView(resultado, fim - inicio);
            BtnProcessar.IsEnabled = true;
        }

        private async Task<String[]> ConsolidarContas(IEnumerable<ContaCliente> contas, IProgress<string> reportadorDeProgresso)
        {
            var tasks = contas.Select(conta =>
            {
                return Task.Factory.StartNew(() =>
                {
                    var resultadoConsolidacao = r_Servico.ConsolidarMovimentacao(conta);

                    reportadorDeProgresso.Report(resultadoConsolidacao);

                    return resultadoConsolidacao;
                });
            });

            var res = await Task.WhenAll(tasks);

            return res;
        }

        private void AtualizarView(IEnumerable<String> result, TimeSpan elapsedTime)
        {
            var tempoDecorrido = $"{ elapsedTime.Seconds }.{ elapsedTime.Milliseconds} segundos!";
            var mensagem = $"Processamento de {result.Count()} clientes em {tempoDecorrido}";

            LstResultados.ItemsSource = result;
            TxtTempo.Text = mensagem;
        }

        private void LimparView()
        {
            LstResultados.ItemsSource = null;
            TxtTempo.Text = null;
            PgsProgresso.Value = 0;
        }

        // Ao usar Async Await, o compilador irá executar a segunda linha em uma Task diferente
        // e a terceira linha será executada após o término da 2 linhas, mas no mesmo contexto da primeira linha
        //btnCalcular.IsEnabled = false;
        //var A = await CalculaRaiz(100);
        //btnCalcular.IsEnabled = true;
    }
}
