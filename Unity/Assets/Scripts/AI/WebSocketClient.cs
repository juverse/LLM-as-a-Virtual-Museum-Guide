using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;


[System.Serializable]
public class AudioDeltaResponse
{
    public string type;
    public string event_id;
    public string response_id;
    public string item_id;
    public int output_index;
    public int content_index;
    public string delta;
}

public class WebSocketClient : MonoBehaviour
{
    //WebSocket URL
    private static readonly Uri uri = new Uri("wss://api.openai.com/v1/realtime?model=gpt-4o-realtime-preview-2024-10-01");

    private ClientWebSocket _clientWebSocket;

    [SerializeField]
    private GameObject receiver;
    
    [SerializeField]
    private String userInstructions;

    
    [SerializeField]
    private String userAge;

    [SerializeField]
    private String responseLanguage;

    private AudioRecorder audioRecorder;
    private InputAction recordAction;
    public InputActionAsset vrInputActions; // Reference to the Input Actions asset
    private WebSocketReceiver receiverSocket;
    private ScreenshotCapture screenshot;
    
    private string focusBlock = "Bitte fokusiere dich bei deinen Antworten auf die Instruktionen die ich dir gegeben habe";
    private async void Start()
    {
        await ConnectAsync();
        audioRecorder = GetComponent<AudioRecorder>();
        screenshot = GetComponent<ScreenshotCapture>();
        GameObject openaireceiver = Instantiate(receiver);
        openaireceiver.name = "WebSocketReceiver";
        receiverSocket = openaireceiver.GetComponent<WebSocketReceiver>();
        receiverSocket.SetWebSocket(_clientWebSocket);
        receiverSocket.SetWebSocketClient(this);

        var actionMap = vrInputActions.FindActionMap("XRI Right Interaction", true);

        // Assign each action to variables for easy access
        recordAction = actionMap.FindAction("AudioRecord", true);

        recordAction.started += OnButtonAPressed;
        recordAction.canceled += OnButtonAReleased;
         await SendInitialMessage();
    }

    public async Task ConnectAsync()
    {
        _clientWebSocket = new ClientWebSocket();
        Debug.Log("Connecting to OpenAI");

        
       // _clientWebSocket.Options.SetRequestHeader("Authorization", "Bearer " + Environment.GetEnvironmentVariable("OPENAI_API_KEY")); sk-5DKTIE0x3IJLsRDbb_K3xA1cFVSPC9wL-48E1wqzikT3BlbkFJFYH2VXtiEgt0ZxGNXni0P4XDZl42MFwOLulZp7bx4A
        _clientWebSocket.Options.SetRequestHeader("Authorization", "Bearer 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");
        
        _clientWebSocket.Options.SetRequestHeader("OpenAI-Beta", "realtime=v1");

        try
        {
            await _clientWebSocket.ConnectAsync(uri, CancellationToken.None);
            Debug.Log("Connected to server.");

           //

        }
        catch (WebSocketException e)
        {
            Debug.Log("WebSocket Exception: " + e.Message);
        }
    }

    private async Task SendInitialMessage()
    {
        var message = new
        {
        type = "session.update",
        session = new
            {
            voice = "ash",
            modalities = new[] { "text", "audio" }, 
            instructions = @"Du bist ein hilfreicher Museumsführer für die Ausstellung Epochal im Herzog Anton Ulrich-Museum in Braunschweig.
                Beim ersten Kontakt begrüßt du den Besucher und stellst dich mit diesem Satz vor: ""Willkommen in der Epochal-Ausstellung des Herzog Anton-Ulrich Museums. Ich beantworte gerne Ihre Fragen zu den Kunstwerken dieser Ausstellung. Ich kann Ihnen allgemeine Informationen zu den Werken und ihren Künstlern geben, oder auf ihre Fragen zu Technik, Komposition und Symbolik eingehen.""
                Deine Aufgabe ist es, altersgerechte Informationen zu geben. 
                Dabei konzentrierst du dich auf das Thema, den Inhalt, die Lichtgestaltung sowie die Geschichte des Kunstwerks und des Künstlers.
                
                Du gibst ausschließlich Informationen zu dem angefügten Kontext weiter. Vom Thema abschweifende Anfragen lehnst Du freundlich und sachte ab und verweist auf die Ausstellung.
                Du bist präzise, knapp, verständlich, höflich und strukturiert. Deine Sprache ist freundlich, respektvoll und ansprechend.
                Versuche das Zuhören zu erleichtern. Antworte stets auf Deutsch und passe deine Sprache dem Alter von 40 Jahren und dem Wissenstand Anfänger an.
                Deine Antworten sind maximal 90 Wörter lang. Schließe mit einer weiterführenden Themenvorschlag ab, wie zum Beispiel: 'Möchtest du mehr über die Geschichte dieses Kunstwerks oder über den Künstler erfahren?'

                Beispiel 1:
                Zielperson: Alter: 6 Jahre, Kenntnisstand: Kunstinteressiert, Sprache: Deutsch, leichte Sätze, einfache Wörter
                Input: Was ist das für ein Bild?
                ""Das Bild vor Dir heißt ""Die Dornenkrönung Christi"". Es wurde von Orazio Gentileschi gemalt. Auf dem Bild sehen wir, wie Soldaten Jesus eine Krone aus spitzen Dornen aufsetzen. Das tut ihm sehr weh! Das Bild zeigt, wie sehr Jesus leidet. Das Licht auf dem Bild macht es noch deutlicher. Der Künstler wollte, dass wir verstehen, wie viel Jesus für die Menschen ertragen hat. Möchtest du etwas über den Künstler wissen?""

                Das ist der angefügte Kontext, rede darüber wenn Du explizit danach gefragt wirst:

                JUDITH MIT DEM HAUPT DES HOLOFERNES
                Um 1616 - 18
                Holz, 120 x 111 cm
                Inv. Nr. GG 87
                Das biblische Buch Judith, einer der apokryphen, nicht ursprünglichen Texte der Bibel, berichtet vom Schicksal der hier zu sehenden jungen Witwe aus der jüdischen Stadt Bethulia. Ihr kleines Volk wurde vom Assyrer-Fürsten Holofernes belagert. Um es zu retten, fasste sie einen Plan. Sie verschaffte sich Zugang zu Holofernes’ Heerlager vor den Toren ihrer Stadt. Mit ihren verführerischen weiblichen Reizen, die Rubens hier deutlich hervorbringt, betörte sie Holofernes und machte ihn betrunken. Nachdem er in den Schlaf gesunken war, schlug sie ihm das Haupt ab und pflanzte es auf den Zinnen der Stadtmauer auf. Als dies am nächsten Morgen die assyrischen Krieger sahen, flohen sie in Panik. Was im Mittelalter als Beispiel der so genannten „Weiberlisten“, der zauberischen Verführungsgewalt der Frauen galt, wurde im Barock als mutige Opfertat gewertet. Judith hatte ihr eigenes Leben aufs Spiel gesetzt, um ihr Volk zu befreien. Rubens, der das Thema mehrfach behandelt hat, erweitert diesen Gedanken in diesem Gemälde noch um einen weiteren Aspekt: Mit eindringlichem Blick scheint Judith, deren gerötete Wangen von ihrer inneren Aufgewühltheit zeugen, den Betrachter zu fragen, wie ihre Tat zu bewerten ist. Beherzt packt sie den abgetrennten Kopf ihres Gegners am Haarschopf, doch ihre blutbefleckte Hand erinnert auch daran, dass nun, in der Folge des Kriegszustandes, eine Bluttat auf ihrer Seele lastet. Rubens hat intensiv an dieser Komposition gearbeitet und sie im Zuge des Werkprozesses umfassend überarbeitet. Die Spuren lassen sich, teils mit bloßem Auge, teils mit technischen Untersuchungen, erkennen: Der Maler vergrößerte das ursprüngliche Format der Holztafel. Links wird der Teil mit Judiths Unterarm hinzugefügt. Ihr rechter Arm scheint jetzt den Schwerthieb noch einmal auszuführen. Die Entschlossenheit ihrer Tat, wie sie der Text schildert, wird so noch deutlicher: „Und sie hieb zweimal in seinen Hals mit all ihrer Kraft.“ Ein weiteres Brett fügte er unter der Hand der alten Dienstmagd mit der Kerze an, zahlreiche Übermalungen folgten. Dabei wird Judiths Dekolleté freizügig geöffnet, um an ihre Liebesnacht mit dem Feind zu erinnern. Die Drehung ihres vorderen Armes, mit dem sie den Kopf greift, wird verstärkt, ein weiter Ärmelbausch betont ihn. Die ursprünglich viel höher angelegte Kerzenflamme ist direkt unter ihrem Ellbogen noch zu sehen. Rubens verarbeitet in diesem Werk von extremer Dramatik neben einer Komposition des holländischen Graphikers Hendrick Goltzius vor allem zahlreiche Anregungen der italienischen Malerei, die er bei einem Aufenthalt 1600–1608 in Italien selbst studiert hat. Ein direktes Vorbild stellt eine ähnlich frontale halbfigurige Judith des venezianischen Malers Paolo Veronese dar. Die starke Kontrastwirkung von Hell und Dunkel, die durch die heimliche nächtliche Situation im Zelt inhaltlich begründet ist, spiegelt jedoch unmittelbar die Kunst des römischen Malers Caravaggio wider, der die europäische Malerei durch seinen dramatischen Realismus in eine neue Richtung lenkte. In seinem Sinn ist auch der Gegensatz zwischen der Schönheit der jungen Frau und der runzeligen Alten entworfen. Rubens bezieht dabei die Struktur der fein geglätteten Oberflächen gegen den breiten pastosen Pinselstrich in die Darstellung ein. In maltechnischer und in inhaltlicher Hinsicht zeigt sich Rubens hier als tonangebender Meister des flämischen Barock.

                DIE DORNENKRÖNUNG CHRISTI
                Um 1610 - 15
                Leinwand, 119,5 x 148,5 cm
                Inv. Nr. GG 805
                Als Gentileschis Dornenkrönung Christi 1977 auf den Kunstmarkt gelangte, konnte das Gemälde für die Sammlungen des Herzog Anton Ulrich-Museums erworben werden und bereichert seitdem die Reihe der italienischen Caravaggisten, zu der u. a. auch ein Werk Bartolomeo Manfredis gehört. Orazio Gentileschi war einer der ersten Nachfolger Caravaggios in Rom, arbeitete also in der neuartigen, dramatisch-realistischen Manier des Meisters. Die größte Nähe zum Schaffen Caravaggios zeigt sich bei Gentileschi in den Jahren nach 1610 und hier insbesondere bei der Braunschweiger Dornenkrönung, die demzufolge in den Jahren zwischen 1610–15 entstanden sein dürfte. Dennoch erweist es sich als schwierig, das konkrete Vorbild für die Darstellung zu benennen. Ein möglicher Ausgangpunkt war Caravaggios 1603/04 entstandene Dornenkrönung im Kunsthistorischen Museum in Wien; von ihr dürfte Gentileschi die Grundzüge der Komposition übernommen haben, wie den mittig sitzenden, in sich zusammengesunkenen Christus und die in großer räumlicher Enge auf ihn eindringenden Schergen. Daneben haben vermutlich auch andere Quellen eine Rolle gespielt, wie eine verlorene Dornenkrönung Caravaggios aus der Sammlung Giustiniani und Bartolomeo Manfredis bildliche Gestaltung des Themas. Gegenüber den Vorlagen intensivierte Gentileschi jedoch den Ausdruck physischer wie psychischer Bedrängnis, der der geschundene Christus ausgesetzt ist: Er rückte die Figur so nah an den Betrachter heran, dass die Knie bald die Bildfläche zu durchstoßen scheinen. Auch sind die Schergen ganz knapp in das Format eingespannt und bilden mit der Figur Christi eine Kreuzform aus Diagonalen und Kurven. Die Aufsicht der Szene und die daraus resultierende Verkürzung der Figuren erhöhen zusätzlich das Bedrängt-Bedrückende der dargestellten Situation. Auch in der Ikonographie wich Gentileschi von Caravaggio und seiner Schule ab, da er im eigentlichen Sinne weder eine Dornenkrönung noch – wie häufig behauptet wurde – eine Verspottung dargestellt hat, sondern den zeitlich vorangehenden Moment, in dem Christus die Dornenkrone vorgehalten wird und er den Stab in die Hand gedrückt bekommt, während der rote Umhang noch auf seinen Knien liegt. Mit diesem ungewöhnlichen wie eindringlichen Gemälde schuf der Maler ein schonungsloses Bild der Leiden des misshandelten und verspotteten Erlösers.

                DIE HOCHZEIT VON PELEUS UND THETIS
                1602
                Kupfer, 31,1 x 41,9 cm
                Inv. Nr. GG 174
                Dargestellt ist die vom römischen Dichter Catull in seinem Carmen 64 beschriebene Geschichte der Hochzeit des griechischen Helden Peleus und der Nereide Thetis, Mutter des Achill. Zu diesem großen Götterfest waren alle olympischen Götter geladen, nur Eris, die Göttin der Zwietracht, nicht. Sie erscheint dennoch und wirft einen goldenen Apfel mit der Aufschrift ""Der Schönsten"" auf die Hochzeitstafel. Paris muss sich zwischen Hera, Athene und Aphrodite entscheiden – oben links in simultaner Darstellung der eigentlich nachzeitigen Szene – und wählt Aphrodite.
                Das Thema ist eines der Lieblingsthemen des späten holländischen Manierismus, da es dem Künstler ermöglicht, seine Virtuosität in der Darstellung des menschlichen unbekleideten Körpers in jeder erdenklichen Drehung und Variation vorzuführen. Der Utrechter Historien-, Genre- und Porträtmaler Wtewael beschäftigt sich in sechs Varianten nahezu über seine gesamte Schaffenszeit mit dem Thema, in kleinfigurigen Kabinettbildern ebenso wie in der Zeichnung. Jede Variante und auch das Braunschweiger Bild zeigt die Auseinandersetzung mit der Komposition des 1587 entstandenen Gemäldes Hochzeit von Amor und Psyche von Bartholomäus Spranger, das durch den Reproduktionsstich des Hendrick Goltzius europaweit verbreitet wurde.
                Dessen Komposition liefert die Bildstruktur: Spiralförmig nach oben ansteigende Wolken werden von dichtgedrängten Figurengruppen in gleichmäßiger Verteilung bevölkert, gleichsam als Zuschauer der zentralen Göttertafel. Im Kontrast zu der nahsichtigen Szenerie in den Wolken öffnet sich darunter der Fernblick auf eine weite irdische Landschaft. Bewegte Diversität der antikisch-nackten Figuren, stark differenzierte Hauttöne, lebendige Lokalfarbigkeit der Szenen auf scheinbar massiven Wolken in Kontrast mit der luftperspektivischen Ferne der Landschaft bewirken zusammen mit der qualitätvollen Feinmalerei – jede Figur ist trotz kleinen Formats durch Attribute gekennzeichnet – die besondere Attraktivität des Bildes, wie schon von dem Zeitgenossen Carel van Mander, selbst Maler und Kunstschriftsteller, bemerkt wurde.
                Das Gemälde ist rechts unten signiert, 1602 datiert. Es wurde vor 1776 erworben.

                DAS MÄDCHEN MIT DEM WEINGLAS
                1658
                Leinwand, 78 x 67 cm
                Links bez.: „JVMeer“ (ligiert)
                Inv. Nr. GG 316
                Johannes Vermeer van Delft scheint eine Szene aus dem Leben des gehobenen Bürgertums im Holland des ""Goldenen Jahrhunderts"" zu schildern. Eine junge Dame prostet uns lächelt lächelnd mit ihrem Weinglas zu. Man könnte glauben, sie bemerke nicht den irritierend bedrängenden Blick des Herrn, der – noch im Mantel – neben ihr steht. Ermunternd führt er ihre Hand, so als könne er es nicht erwarten, bis sie von dem Wein koste und der Alkohol seine Wirkung auf die junge Frau entfalte. Rätselhaft erscheint in dieser Szene die Rolle des unbeteiligt wirkenden Kompagnons, der an einem Tisch links im Hintergrund des Raumes sitzt. Wartet er, leicht eingenickt, auf das Ende der Konversation – oder will er das Paar mit gespielter Abwesenheit nicht kompromittieren? Als stiller Beobachter kommentiert hingegen der Herr auf dem altertümlichen Porträtgemälde das Geschehen. Schon für zeitgenössische Käufer holländischer Genrebilder bestand deren Reiz in ihren mehrdeutigen Anspielungen, die sich aus immer neuen Kombinationen tradierter Bildsymbole ergaben. Man hat vorgeschlagen, in unserer Szene einfach die Unterweisung einer jungen Dame im kultivierten Betragen zu sehen. Ganz stolz scheint sie auf ihre Eleganz und wie angemessen sie das Weinglas hält. Ihr kostbares rotes Seidenkleid war eigentlich nur für besondere Ereignisse gedacht: Im Alltag bevorzugten Holländerinnen bequemere Kleidung statt eng geschnürter Korsagen Doch der „Lehrer“ verhält sich keineswegs comme il faut. Er verletzt die Regeln der Distanz, die Reputation des Mädchens scheint gefährdet. Die geschälte Zitrone konnte von Zeitgenossen als Symbol verletzter Unschuld gedeutet werden – selbst wenn Zitronensaft als Zutat im Wein damals gebräuchlich war. Das Bild der auffällig ins Zimmer geklappten Fensterscheibe erinnert in diesem Zusammenhang an die emblematische Figur der Temperantia, die mit einem Zaumzeug zur Mäßigung mahnt. Zwar zeigt sich bei genauem Hinsehen hier nur eine Wappenhalterin, die ein Familienwappen an fliegenden Bändern fasst, doch Vermeer wird das anspielende Motiv nicht zufällig gewählt haben. Den Blick des Mädchens wendet er davon ab, zu uns hin: Zeugt ihr Lächeln also von kindlicher Unschuld oder von einem riskanten Spiel mit den Avancen des Mannes? Vermeers kunstvoll gewobene, andeutende Erzählweise erzeugt Spannung. Eine besondere poetische Kraft entsteht aber erst durch die einzigartige Weise, wie Vermeer den Charakter reinen, nördlichen Tageslichts schildert. Niemand sonst zeigt so klar, wie sich die Farbigkeit der Dinge im Zusammenspiel mit ihrer Umgebung wandelt. Vermeer löst sich in atemberaubendem Maß von dem, was wir über die Farbe eines Gegenstandes zu wissen glauben und überzeugt uns gerade auf diese Weise. So lässt er das Weiß der Serviette oder des Delfter Fayencekruges neben dem ultramarinblauen Tischtuch blau schattiert erscheinen oder er taucht eine Ärmelspitze der Dame in goldenes Ocker, während er die andere ebenfalls bläulich schimmern lässt. Die Dominanz des Farbdreiklangs Rot-Blau-Gelb wird dabei in unserem Bild zum artistischen Prinzip. Selbst Schattenzonen in dem virtuos gemalten roten Seidenkleid werden mit kostbarem Ultramarinblau und leuchtendem Gelb gebildet. Vermeers Thema ist das Transitorische, die Veränderlichkeit der für uns zunächst konstant erscheinenden sichtbaren Welt. Die Erfahrung des stets wechselnden Lichts ist dafür der unmittelbarste sinnliche Ausdruck."
            }
        };
        

        string jsonMessage = JsonSerializer.Serialize(message);
        byte[] messageBytes = Encoding.UTF8.GetBytes(jsonMessage);

        await _clientWebSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        Debug.Log("Sent initial message: " + jsonMessage);
    }

    public async Task SendTextUser(string prompt)
    {
        Debug.Log("Sent audio data as binary.");
        if (_clientWebSocket.State == WebSocketState.Open)
        {
            Debug.Log("_clientWebSocket.State " + _clientWebSocket.State);

            // First message: conversation.item.create
            var eventMessage = new
            {
                type = "conversation.item.create",
                item = new
                {
                    type = "message",
                    role = "user",
                    content = new[]
                    {
                        new
                        {
                            type = "input_text",
                            text = prompt // or use prompt variable if needed
                        }
                    }
                }
            };

            string eventJson = JsonSerializer.Serialize(eventMessage);
            byte[] eventBytes = Encoding.UTF8.GetBytes(eventJson);

            // Send the first event
            await _clientWebSocket.SendAsync(new ArraySegment<byte>(eventBytes), WebSocketMessageType.Text, true, CancellationToken.None);

            await CreateResponds();
        }

        Debug.Log("Sent audio data as binary.");
    }

    public async Task SendTextSystem(string prompt)
    {
        Debug.Log("Sent audio data as binary.");
        if (_clientWebSocket.State == WebSocketState.Open)
        {
            Debug.Log("_clientWebSocket.State " + _clientWebSocket.State);

            // First message: conversation.item.create
            var eventMessage = new
            {
                type = "conversation.item.create",
                item = new
                {
                    type = "message",
                    role = "system",
                    content = new[]
                    {
                        new
                        {
                            type = "input_text",
                            text = prompt // or use prompt variable if needed
                        }
                    }
                }
            };

            string eventJson = JsonSerializer.Serialize(eventMessage);
            byte[] eventBytes = Encoding.UTF8.GetBytes(eventJson);

            // Send the first event
            await _clientWebSocket.SendAsync(new ArraySegment<byte>(eventBytes), WebSocketMessageType.Text, true, CancellationToken.None);

            await CreateResponds();
        }

        Debug.Log("Sent audio data as binary.");
    }

    public async Task SendAudio(string base64AudioData)
    {
        
        if (_clientWebSocket.State == WebSocketState.Open)
        {
            Debug.Log("_clientWebSocket.State " + _clientWebSocket.State);

            // First message: conversation.item.create
            var eventMessage = new
            {
                type = "conversation.item.create",
                item = new
                {
                    type = "message",
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "input_audio",
                            audio = base64AudioData  // or use prompt variable if needed
                        }
                    }
                }
            };

            string eventJson = JsonSerializer.Serialize(eventMessage);
            byte[] eventBytes = Encoding.UTF8.GetBytes(eventJson);

            // Send the first event
            await _clientWebSocket.SendAsync(new ArraySegment<byte>(eventBytes), WebSocketMessageType.Text, true, CancellationToken.None);

            await CreateResponds();
        }

        Debug.Log("Sent audio data as binary.");
    }

    private async Task CreateResponds()
    {
        // Second message: response.create
        var responseMessage = new
        {
            type = "response.create"
        };

        string responseJson = JsonSerializer.Serialize(responseMessage);
        byte[] responseBytes = Encoding.UTF8.GetBytes(responseJson);

        // Send the second message
        await _clientWebSocket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public async Task SendInterrupt()
    {
        if (_clientWebSocket.State == WebSocketState.Open)
        {
            //await ClearAudioBuffer(); 

            await ResponseCancel();
            receiverSocket.InterruptAudioStream();
            //await ResponseTruncate();
        }
    }

    private async Task ResponseTruncate()
    {
         Debug.LogWarning("ResponseTruncate " + receiverSocket.getResponseEventId());
        var truncateMessage = new
        {
            event_id = receiverSocket.getResponseEventId(),
            //type = "response.cancel",
            type = "conversation.item.truncate"

        };

        string json = JsonSerializer.Serialize(truncateMessage);
        byte[] response = Encoding.UTF8.GetBytes(json);

        // Send the second message
        await _clientWebSocket.SendAsync(new ArraySegment<byte>(response), WebSocketMessageType.Text, true, CancellationToken.None);

        //await CreateResponds();
    }

    private async Task ClearAudioBuffer()
    {
        Debug.Log("Send interrupt " + receiverSocket.getResponseEventId());
        var eventMessage = new
        {
            event_id = receiverSocket.getResponseEventId(),
            type = "input_audio_buffer.clear"
            //type = "conversation.item.truncate"

        };

        string responseJson = JsonSerializer.Serialize(eventMessage);
        byte[] responseBytes = Encoding.UTF8.GetBytes(responseJson);

        // Send the second message
        await _clientWebSocket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        //await CreateResponds();
    }

    private async Task ResponseCancel()
    {
        Debug.LogWarning("ResponseCancel " + receiverSocket.getResponseEventId());
        var eventMessage = new
        {
            event_id = receiverSocket.getResponseEventId(),
            type = "response.cancel",
            //type = "conversation.item.truncate"

        };

        string responseJson = JsonSerializer.Serialize(eventMessage);
        byte[] responseBytes = Encoding.UTF8.GetBytes(responseJson);

        // Send the second message
        await _clientWebSocket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        //await CreateResponds();
    }

    private void Update()
    {
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {

            _ = SendTextUser("ja gerne, erzähle mir mehr darüber");
            Debug.Log("Sending done");
           
        }
        else if(Keyboard.current.iKey.wasPressedThisFrame)
        {
             _ = SendInterrupt();
            Debug.LogWarning("Interrupting done");
        }
        else if (Keyboard.current.aKey.wasPressedThisFrame)
        {

            _ = SendTextUser("nein danke, ich würde gerne etwas anderes erfahren");
            Debug.Log("Sending done");
        }
        else if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            
            audioRecorder.StartRecording();
            Debug.Log("Recording started...");
        }
        else if (Keyboard.current.rKey.wasReleasedThisFrame)
        {
            audioRecorder.StopRecording();

            // Process the audio and send it to OpenAI
            string filePath = audioRecorder.GetFilePath();
            Debug.Log("filePath "+filePath);    
            byte[] audioData = File.ReadAllBytes(filePath);

            string base64AudioData = Convert.ToBase64String(audioData);
            Debug.Log(base64AudioData);
            _ = SendAudio(base64AudioData);
        }

        else if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            string filePath = Path.Combine(Application.persistentDataPath + "/Assets/Generated", "audioInput.wav");
            byte[] audioData = File.ReadAllBytes(filePath);
            string base64AudioData = Convert.ToBase64String(audioData);
            Debug.Log(base64AudioData);
            _ = SendAudio(base64AudioData);

        }

    }

    private void OnButtonAPressed(InputAction.CallbackContext context)
    {
        Debug.Log("Sending interrupt"); 
        _ = SendInterrupt();
        audioRecorder.StartRecording();
        Debug.Log("Recording started...");
        //screenshot.TakeScreenshot();
        // Perform trigger-related actions here
    }

    private void OnButtonAReleased(InputAction.CallbackContext context)
    {
        audioRecorder.StopRecording();
        Debug.Log("Recording stopped...");
        // Process the audio and send it to OpenAI
        string filePath = @""+audioRecorder.GetFilePath();
        Debug.Log("filePath " + filePath);
        byte[] audioData = File.ReadAllBytes(filePath);
        
        string base64AudioData = Convert.ToBase64String(audioData);
        Debug.Log("String length "+base64AudioData.Length);
       // if(base64AudioData.Length > 50000)
            _ = SendAudio(base64AudioData);
    }


    public async Task CloseAsync()
    {
        if (_clientWebSocket != null && _clientWebSocket.State == WebSocketState.Open)
        {
            await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closed connection", CancellationToken.None);
        }
        Debug.Log("WebSocket connection closed.");
    }
}
