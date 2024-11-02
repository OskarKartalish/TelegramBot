using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

class Program
{
    static ITelegramBotClient? botClient;
    static Dictionary<long, bool> waitingForNumber = new Dictionary<long, bool>();
    static Dictionary<long, string> selectedProductName = new Dictionary<long, string>();
    static Dictionary<long, decimal> selectedProductPrice = new Dictionary<long, decimal>();

    static async Task Main()
    {
        botClient = new TelegramBotClient("TOKEN");

        var me = await botClient.GetMeAsync();
        Console.WriteLine($"Бот {me.FirstName} запущено");

        var cts = new CancellationTokenSource();
        botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() },
            cancellationToken: cts.Token
        );

        Console.WriteLine("Натисніть будь-яку клавішу для завершення");
        Console.ReadKey();
        cts.Cancel();
    }

    static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.Message && update.Message!.Text != null &&
            waitingForNumber.TryGetValue(update.Message.Chat.Id, out bool isWaiting) && isWaiting)
        {
            if (int.TryParse(update.Message.Text, out int quantity))
            {
                // Отримуємо вибраний продукт та його ціну для поточного користувача
                if (selectedProductName.TryGetValue(update.Message.Chat.Id, out string? productName) &&
                    selectedProductPrice.TryGetValue(update.Message.Chat.Id, out decimal productPrice))
                {
                    // Обробляємо замовлення з кількістю
                    await HandleMessageAsync(update.Message, productName, productPrice, quantity, cancellationToken);

                    // Очищаємо стан очікування та вибір продукту після успішного замовлення
                    waitingForNumber[update.Message.Chat.Id] = false;
                    selectedProductName.Remove(update.Message.Chat.Id);
                    selectedProductPrice.Remove(update.Message.Chat.Id);
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: update.Message.Chat.Id,
                    text: "Будь ласка, введіть коректне число.",
                    cancellationToken: cancellationToken
                );
            }
            return;
        }

        if (update.Type == UpdateType.Message && update.Message!.Text != null)
        {
            var message = update.Message;
            Console.WriteLine($"Отримано повідомлення: {message.Text}");

            // Перевірка тексту повідомлення та надсилання відповідної фотографії й клавіатури
            if (message.Text == "Auto" || message.Text == "Metis" || message.Text == "Agave")
            {
                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithCallbackData($"Замовити {message.Text}", $"order_{message.Text.ToLower()}")
                });

                string caption = message.Text switch
                {
                    "Auto" => "Auto Coin - це агрегація для фермерства.\r\n\r\n📉 Ціна однієї BRUT монети - 649₽\r\n📈 Ціна на біржі - 870.00₽",
                    "Metis" => "Metis Coin – монета безпеки.\r\n\r\n📉 Ціна однієї BRUT монети - 1090₽\r\n📈 Ціна на біржі - 1856.00₽",
                    "Agave" => "Agave Coin – токен для інвестицій у розвиток корисних культур.\r\n\r\n📉 Ціна однієї BRUT монети - 1390₽\r\n📈 Ціна на біржі - 2618.00₽",
                    _ => ""
                };

                await botClient.SendPhotoAsync(
                    chatId: message.Chat.Id,
                    photo: new InputFileStream(System.IO.File.OpenRead($"D:\\c#\\tgbot\\ConsoleApp4\\ConsoleApp4\\{message.Text.ToLower()}.png")),
                    caption: caption + "\r\n\r\nДля замовлення, натисніть кнопку нижче",
                    replyMarkup: inlineKeyboard,
                    cancellationToken: cancellationToken
                );
            }
        }
        else if (update.Type == UpdateType.CallbackQuery)
        {
            var callbackQuery = update.CallbackQuery;
            Console.WriteLine($"Отримано зворотний виклик: {callbackQuery?.Data}");

            string productName = callbackQuery?.Data switch
            {
                "order_auto" => "Auto",
                "order_metis" => "Metis",
                "order_agave" => "Agave",
                _ => ""
            };

            decimal productPrice = productName switch
            {
                "Auto" => 649,
                "Metis" => 1090,
                "Agave" => 1390,
                _ => 0
            };

            if (!string.IsNullOrEmpty(productName))
            {
                selectedProductName[callbackQuery.Message!.Chat.Id] = productName;
                selectedProductPrice[callbackQuery.Message.Chat.Id] = productPrice;

                await botClient.AnswerCallbackQueryAsync(
                    callbackQueryId: callbackQuery.Id,
                    text: $"Ви вибрали замовити {productName}!",
                    cancellationToken: cancellationToken
                );
                await botClient.SendTextMessageAsync(
                    chatId: callbackQuery.Message.Chat.Id,
                    text: $"Дякуємо за замовлення {productName}!\nСкільки ви хочете замовити?\r\n\r\nВведіть кількість:",
                    cancellationToken: cancellationToken
                );

                waitingForNumber[callbackQuery.Message.Chat.Id] = true;
            }
        }
    }

    static async Task HandleMessageAsync(Message message, string productName, decimal productPrice, int quantity, CancellationToken cancellationToken)
    {
        decimal totalPrice = productPrice * quantity;

        string orderConfirmation = $"🛒 ОФОРМЛЕННЯ\n\n" +
            $"{DateTime.Now:yyyy-MM-dd}\n" +
            $"🔹 ID заказчика - {message.From!.Id}\n" +
            $"🔹 Выбранная монета - {productName.ToUpper()}\n" +
            $"🔹 Количество монет - {quantity}\n" +
            $"🛒 Сумма заказа: {totalPrice}₽\n\n" +
            $"⚠️ Для получения монеты и оплаты заказа, перешлите данное сообщение менеджеру\n" +
            $"👨‍💼 Менеджер: @Efdsfsfsfd";

        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: orderConfirmation,
            cancellationToken: cancellationToken
        );
    }

    static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Сталася помилка: {exception.Message}");
        return Task.CompletedTask;
    }
}
