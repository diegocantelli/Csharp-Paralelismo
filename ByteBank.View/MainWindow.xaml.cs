﻿using ByteBank.Core.Model;
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

        private async void BtnProcessar_Click(object sender, RoutedEventArgs e)
        {
            BtnProcessar.IsEnabled = false;

            var contas = r_Repositorio.GetContaClientes();

            PgsProgresso.Maximum = contas.Count();
            LimparView();

            var inicio = DateTime.Now;

            //Await -> Irá aguardar o resultado da task ConsolidarContas
            var resultado = await ConsolidarContas(contas);

            var fim = DateTime.Now;
            AtualizarView(resultado, fim - inicio);
            BtnProcessar.IsEnabled = true;
        }

        private async Task<String[]> ConsolidarContas(IEnumerable<ContaCliente> contas)
        {
            // Neste momento o contexto da thread é o da UI
            // Só muda o contexto quando este trecho for executado dentro de uma Task
            var taskSchedulerGui = TaskScheduler.FromCurrentSynchronizationContext();

            var tasks = contas.Select(conta =>
            {
                return Task.Factory.StartNew(() => {
                   var resultadoConsolidacao = r_Servico.ConsolidarMovimentacao(conta);

                    // Incrementa a barra de progresso, passando o contexto da Thread de UI como parâmetro
                    Task.Factory.StartNew(() => PgsProgresso.Value++,
                        CancellationToken.None,
                        TaskCreationOptions.None,
                        taskSchedulerGui
                    );
                    
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
