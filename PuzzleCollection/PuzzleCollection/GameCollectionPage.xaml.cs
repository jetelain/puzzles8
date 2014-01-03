using PuzzleCollection.Games.Pattern;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;


// Pour en savoir plus sur le modèle d'élément Page Éléments groupés, consultez la page http://go.microsoft.com/fwlink/?LinkId=234231

namespace PuzzleCollection
{
    /// <summary>
    /// Page affichant une collection groupée d'éléments.
    /// </summary>
    public sealed partial class GameCollectionPage : PuzzleCollection.Common.LayoutAwarePage
    {
        public GameCollectionPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Remplit la page à l'aide du contenu passé lors de la navigation. Tout état enregistré est également
        /// fourni lorsqu'une page est recréée à partir d'une session antérieure.
        /// </summary>
        /// <param name="navigationParameter">Valeur de paramètre passée à
        /// <see cref="Frame.Navigate(Type, Object)"/> lors de la requête initiale de cette page.
        /// </param>
        /// <param name="pageState">Dictionnaire d'état conservé par cette page durant une session
        /// antérieure. Null lors de la première visite de la page.</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            this.DefaultViewModel["Groups"] = GameCollection.GamesList;
        }

        /// <summary>
        /// Invoqué lorsqu'un utilisateur clique sur un élément appartenant à un groupe.
        /// </summary>
        /// <param name="sender">GridView (ou ListView lorsque l'état d'affichage de l'application est Snapped)
        /// affichant l'élément sur lequel l'utilisateur a cliqué.</param>
        /// <param name="e">Données d'événement décrivant l'élément sur lequel l'utilisateur a cliqué.</param>
        void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            this.Frame.Navigate(typeof(GamePage), ((GameInfo)e.ClickedItem).GameId);
        }
    }
}
