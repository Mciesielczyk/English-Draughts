using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp1;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>

public partial class MainWindow : Window
{
    private Button selectedButton = null; 
    private bool isBlackTurn = false; // True - ruch czarnych, False - ruch białych

    public MainWindow()
    {
        InitializeComponent();
        CreateBoard();
        //MakeBotMove();
        
    }
    private string GetCurrentBoardState()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(isBlackTurn ? "Tura: czarnych" : "Tura: białych");

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                Button button = FindButtonByTag(row, col);
                if (button?.Content is Ellipse piece)
                {
                    string pieceType = piece.Fill.ToString() == new SolidColorBrush(Colors.Black).ToString() ? "B" : "W";
                    if (IsKing(button)) pieceType += "K";
                    sb.Append(pieceType);
                }
                else
                {
                    sb.Append(".");
                }
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private async Task<string> GetAiHint()
    {
        AiService aiService = new AiService();
        string gameState = GetCurrentBoardState();
        string prompt = "Jaki ruch powinien wykonać gracz w tej sytuacji?\n" + gameState;

        string aiResponse = await aiService.GetAiResponse(prompt);
        if (aiService == null)
        {
            MessageBox.Show("Błąd: AI Service nie został poprawnie zainicjalizowany.");
            return "a";
        }

        return aiResponse;
    }

    private async void AiHintButton_Click(object sender, RoutedEventArgs e)
    {
        string hint = await GetAiHint();
        if (string.IsNullOrEmpty(hint))
        {
            MessageBox.Show("AAaAAA");
        }
        AiHintTextBlock.Text = "Podpowiedź: " + hint;
    }

    private void MakeBotMove()
    {
        List<Button> botPieces = new List<Button>();

        foreach (Button button in GameBoard.Children)
        {
            if (button.Content is Ellipse piece)
            {
                bool isBlackPiece = piece.Fill.ToString() == new SolidColorBrush(Colors.Black).ToString();
                if (isBlackTurn && isBlackPiece)
                {
                    botPieces.Add(button);
                }
            }
        }

        if (botPieces.Count == 0)
        {
            MessageBox.Show("Bot nie ma pionków do ruchu!");
            return;
        }

        var captureMoves = new List<Tuple<Button, Button, Button>>();
        var regularMoves = new List<Tuple<Button, Button>>();

        foreach (Button botPiece in botPieces)
        {
            string[] pos = botPiece.Tag.ToString().Split(',');
            int row = int.Parse(pos[0]);
            int col = int.Parse(pos[1]);

            bool isKing = IsKing(botPiece);

            if (isKing)
            {
                foreach (int rowDir in new[] { -1, 1 })
                {
                    foreach (int colDir in new[] { -1, 1 })
                    {
                        int newRow = row + rowDir;
                        int newCol = col + colDir;
                        if (IsWithinBoard(newRow, newCol))
                        {
                            Button target = FindButtonByTag(newRow, newCol);
                            if (target != null && target.Content == null)
                            {
                                regularMoves.Add(Tuple.Create(botPiece, target));
                            }
                        }

                        int captureRow = row + 2 * rowDir;
                        int captureCol = col + 2 * colDir;
                        if (IsWithinBoard(captureRow, captureCol))
                        {
                            Button middle = FindButtonByTag(row + rowDir, col + colDir);
                            Button target = FindButtonByTag(captureRow, captureCol);
                            if (middle?.Content is Ellipse middlePiece && target?.Content == null)
                            {
                                bool isOpponent = middlePiece.Fill.ToString() == new SolidColorBrush(Colors.Gray).ToString();
                                if (isOpponent)
                                {
                                    captureMoves.Add(Tuple.Create(botPiece, target, middle));
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                int direction = 1;

                foreach (int move in new[] { -1, 1 })
                {
                    int newRow = row + direction;
                    int newCol = col + move;
                    if (IsWithinBoard(newRow, newCol))
                    {
                        Button target = FindButtonByTag(newRow, newCol);
                        if (target != null && target.Content == null)
                        {
                            regularMoves.Add(Tuple.Create(botPiece, target));
                        }
                    }

                    int captureRow = row + 2 * direction;
                    int captureCol = col + 2 * move;
                    if (IsWithinBoard(captureRow, captureCol))
                    {
                        Button middle = FindButtonByTag(row + direction, col + move);
                        Button target = FindButtonByTag(captureRow, captureCol);
                        if (middle?.Content is Ellipse middlePiece && target?.Content == null)
                        {
                            bool isOpponent = middlePiece.Fill.ToString() == new SolidColorBrush(Colors.Gray).ToString();
                            if (isOpponent)
                            {
                                captureMoves.Add(Tuple.Create(botPiece, target, middle));
                            }
                        }
                    }
                }
            }
        }

        Random rand = new Random();
        if (captureMoves.Count > 0)
        {
            var move = captureMoves[rand.Next(captureMoves.Count)];
            selectedButton = move.Item1;
            move.Item3.Content = null; 
            MovePiece(move.Item2);
        }
        else if (regularMoves.Count > 0)
        {
            var move = regularMoves[rand.Next(regularMoves.Count)];
            selectedButton = move.Item1;
            MovePiece(move.Item2);
        }
        else
        {
            MessageBox.Show("Bot nie znalazł dostępnego ruchu!");
        }
    }

    private bool IsWithinBoard(int row, int col)
    {
        return row >= 0 && row < 8 && col >= 0 && col < 8;
    }


    private void Button_Click(object sender, RoutedEventArgs e)
    {
        Button clickedButton = (Button)sender;
        string[] position = clickedButton.Tag.ToString().Split(',');
        int row = int.Parse(position[0]);
        int col = int.Parse(position[1]);

        if (selectedButton == null && clickedButton.Content != null)
        {
            Ellipse piece = clickedButton.Content as Ellipse;
            if (piece != null)
            {
                bool isBlackPiece = piece.Fill.ToString() == new SolidColorBrush(Colors.Black).ToString();
                bool isWhitePiece = piece.Fill.ToString() == new SolidColorBrush(Colors.Gray).ToString();

                if ((isBlackTurn && isBlackPiece) || (!isBlackTurn && isWhitePiece))
                {
                    selectedButton = clickedButton;
                    clickedButton.BorderBrush = new SolidColorBrush(Colors.Red);
                }
            }
        }
        else if (selectedButton != null && clickedButton.Content == null)
        {
            string[] oldPosition = selectedButton.Tag.ToString().Split(',');
            int oldRow = int.Parse(oldPosition[0]);
            int oldCol = int.Parse(oldPosition[1]);

            if (IsKing(selectedButton))
            {
                if (IsValidKingMove(oldRow, oldCol, row, col))
                {
                    MovePiece(clickedButton);
                }
            }
            else
            {
                int rowDiff = row - oldRow;
                int colDiff = Math.Abs(col - oldCol);

                if (colDiff == 1 && ((isBlackTurn && rowDiff == 1) || (!isBlackTurn && rowDiff == -1)))
                {
                    MovePiece(clickedButton);
                }
                else if (colDiff == 2 && ((isBlackTurn && rowDiff == 2) || (!isBlackTurn && rowDiff == -2)))
                {
                    int middleRow = (oldRow + row) / 2;
                    int middleCol = (oldCol + col) / 2;

                    Button middleButton = FindButtonByTag(middleRow, middleCol);
                    if (middleButton?.Content is Ellipse middlePiece)
                    {
                        bool isBlackPiece = middlePiece.Fill.ToString() == new SolidColorBrush(Colors.Black).ToString();
                        bool isWhitePiece = middlePiece.Fill.ToString() == new SolidColorBrush(Colors.Gray).ToString();

                        if ((isBlackTurn && isWhitePiece) || (!isBlackTurn && isBlackPiece))
                        {
                            middleButton.Content = null;
                            MovePiece(clickedButton);
                        }
                    }
                }
                else
                {
                    ResetSelection();
                }
            }
        }
    }

    private bool IsKing(Button button)
    {
        return button.Content is Ellipse piece && piece.StrokeThickness == 3;
    }

    private bool IsValidKingMove(int oldRow, int oldCol, int newRow, int newCol)
    {
        int rowDiff = Math.Abs(newRow - oldRow);
        int colDiff = Math.Abs(newCol - oldCol);
        return rowDiff == colDiff;
    }

    private void ResetSelection()
    {
        if (selectedButton != null)
        {
            selectedButton.BorderBrush = null;
            selectedButton = null;
        }
    }

    private void MovePiece(Button targetButton)
    {
        targetButton.Content = selectedButton.Content;
        selectedButton.Content = null;
        selectedButton.BorderBrush = null;
        selectedButton = null;

        string[] position = targetButton.Tag.ToString().Split(',');
        int row = int.Parse(position[0]);

        if ((isBlackTurn && row == 7) || (!isBlackTurn && row == 0))
        {
            PromoteToKing(targetButton);
        }

        CheckWinCondition();
        isBlackTurn = !isBlackTurn;
        if (isBlackTurn) // Jeśli teraz jest tura bota, wykonaj ruch
        {
            MakeBotMove();
        }
    }

    private void CheckWinCondition()
    {
        int blackCount = 0, whiteCount = 0;

        foreach (Button button in GameBoard.Children)
        {
            if (button.Content is Ellipse piece)
            {
                if (piece.Fill.ToString() == new SolidColorBrush(Colors.Black).ToString())
                    blackCount++;
                else if (piece.Fill.ToString() == new SolidColorBrush(Colors.Gray).ToString())
                    whiteCount++;
            }
        }

        if (blackCount == 0)
        {
            MessageBox.Show("Białe wygrały!");
            Application.Current.Shutdown();
        }
        else if (whiteCount == 0)
        {
            MessageBox.Show("Czarne wygrały!");
            Application.Current.Shutdown();
        }
    }

    private Button FindButtonByTag(int row, int col)
    {
        foreach (Button button in GameBoard.Children)
        {
            string[] position = button.Tag.ToString().Split(',');
            int buttonRow = int.Parse(position[0]);
            int buttonCol = int.Parse(position[1]);

            if (buttonRow == row && buttonCol == col)
            {
                return button;
            }
        }
        return null;
    }

    private void PromoteToKing(Button button)
    {
        if (button.Content is Ellipse piece)
        {
            piece.Stroke = new SolidColorBrush(Colors.Gold);
            piece.StrokeThickness = 3;
        }
    }

    private void CreateBoard()
    {
        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                Button button = new Button
                {
                    Margin = new Thickness(0),
                    Width = 50,
                    Height = 50,
                    Tag = string.Format("{0},{1}", row, col),
                    Background = (row + col) % 2 == 0 ? new SolidColorBrush(Colors.Black) : new SolidColorBrush(Colors.White)
                };
                button.Click += Button_Click;

                if (row < 3 && (row + col) % 2 != 0)
                {
                    button.Content = new Ellipse { Fill = new SolidColorBrush(Colors.Black), Width = 30, Height = 30 };
                }
                else if (row > 4 && (row + col) % 2 != 0)
                {
                    button.Content = new Ellipse { Fill = new SolidColorBrush(Colors.Gray), Width = 30, Height = 30 };
                }

                Grid.SetRow(button, row);
                Grid.SetColumn(button, col);
                GameBoard.Children.Add(button);
            }
        }
    }
}
