// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Evita duplo clique em qualquer formulário (ex.: criar ticket/usuário/empresa/andamento)
// desabilitando o botão de envio assim que o formulário é de fato submetido. A checagem de
// event.defaultPrevented é defensiva: nenhuma página carrega validação client-side hoje, mas
// se isso mudar no futuro, não queremos desabilitar o botão quando a submissão foi cancelada.
document.addEventListener('submit', function (event) {
    if (event.defaultPrevented) {
        return;
    }

    var button = event.target.querySelector('button[type="submit"]');
    if (button) {
        setTimeout(function () {
            button.disabled = true;
        }, 0);
    }
});
