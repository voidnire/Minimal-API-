namespace MinimalApi2.Models;
public class TB_UTILIZACAO : Notifiable<Notification>
{
    [Key]
    public string? CD_UTILIZACAO { get; set; }
    public string? TX_DESCRICAO { get; set; }

    public TB_UTILIZACAO(string cD_UTILIZACAO, string tX_DESCRICAO)
    {
        CD_UTILIZACAO = cD_UTILIZACAO;
        TX_DESCRICAO = tX_DESCRICAO;


        Validate();
    }

    public void Validate()
    {
        var contract = new Contract<TB_UTILIZACAO>() //da pra ignorar essa classe usando entity frm
            .IsNotNullOrEmpty(CD_UTILIZACAO, "CD_UTILIZACAO", "Código é obrigatório")
            .IsLowerOrEqualsThan(CD_UTILIZACAO, 2, "CD_UTILIZACAO")
            .IsLowerThan(TX_DESCRICAO, 50, "TX_DESCRICAO")
            .IsNotNullOrEmpty(TX_DESCRICAO, "TX_DESCRICAO", "Descrição é obrigatória");
        AddNotifications(contract);
    }
}
