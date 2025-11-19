// TRUE OR FALSE ARENA - Game Logic

// Pontuação e Gamificação
let totalScore = 0;
let correctAnswers = 0;
let totalQuestions = 0;
let currentStreak = 0;
let bestStreak = 0;
let multiplier = 1;

// Controle do jogo
let timerSeconds = 0;
let timerInterval = null;
let currentQuestion = null;
let questionsQueue = [];

// Sistema de pontuação
const POINTS = {
    CORRECT_BASE: 100,
    STREAK_BONUS: 50,
    SPEED_BONUS_MAX: 50,
    WRONG_PENALTY: -25,
    COMBO_THRESHOLDS: [3, 5, 10, 15, 20]
};

// Sistema de níveis
const LEVELS = [
    { name: "Iniciante", minScore: 0, icon: "🌱" },
    { name: "Aprendiz", minScore: 500, icon: "📚" },
    { name: "Conhecedor", minScore: 1000, icon: "🎓" },
    { name: "Expert", minScore: 2000, icon: "⭐" },
    { name: "Mestre", minScore: 3500, icon: "👑" },
    { name: "Lendário", minScore: 5000, icon: "🏆" }
];

let currentLevel = LEVELS[0];

// Initialize game on page load
document.addEventListener('DOMContentLoaded', function() {
    startGame();
});

async function startGame() {
    // Reset de estatísticas
    totalScore = 0;
    correctAnswers = 0;
    totalQuestions = 0;
    currentStreak = 0;
    bestStreak = 0;
    multiplier = 1;
    timerSeconds = 0;
    questionsQueue = [];
    currentLevel = LEVELS[0];
    
    updateGameStats();
    startTimer();
    
    // Clear chat area except first bot message
    const chatArea = document.getElementById('chatArea');
    chatArea.innerHTML = `
        <div class="message bot-message">
            <div class="bot-icon">
                <div class="bot-face">
                    <div class="bot-eyes">
                        <span class="eye"></span>
                        <span class="eye"></span>
                    </div>
                    <div class="bot-mouth"></div>
                </div>
            </div>
            <div class="message-bubble">
                <p>Bem-vindo! Pronto para o desafio? Vamos começar!</p>
            </div>
        </div>
    `;
    
    // Carregar perguntas do backend
    await loadQuestionsFromBackend();
    
    setTimeout(() => {
        loadNextQuestion();
    }, 1000);
}

async function loadQuestionsFromBackend() {
    try {
        console.log('Iniciando carregamento de perguntas...');
        const response = await fetch('/?handler=Questions&count=10');
        
        console.log('Resposta recebida. Status:', response.status, 'OK:', response.ok);
        
        if (!response.ok) {
            const errorText = await response.text();
            console.error('Erro HTTP:', response.status, errorText);
            throw new Error(`Erro HTTP: ${response.status} - ${errorText}`);
        }
        
        const data = await response.json();
        console.log('JSON parseado:', data);
        
        if (!Array.isArray(data)) {
            console.error('Dados não são um array:', data);
            throw new Error('Formato de resposta inválido');
        }
        
        questionsQueue = data;
        console.log(`✅ ${questionsQueue.length} perguntas carregadas com sucesso`);
        
        if (questionsQueue.length > 0) {
            console.log('Primeira pergunta:', questionsQueue[0]);
        }
        
        if (questionsQueue.length === 0) {
            throw new Error('Nenhuma pergunta foi carregada do servidor');
        }
    } catch (error) {
        console.error('❌ Erro ao carregar perguntas:', error);
        addBotMessage(`⚠️ Erro ao carregar perguntas: ${error.message}`);
        disableButtons();
    }
}

async function loadNextQuestion() {
    if (questionsQueue.length === 0) {
        console.log('Sem mais perguntas na fila');
        endGame();
        return;
    }
    
    // Pegar a próxima pergunta da fila
    currentQuestion = questionsQueue.shift();
    
    if (!currentQuestion) {
        console.error('Pergunta não pode ser undefined');
        addBotMessage('❌ Erro ao carregar pergunta. Tente novamente.');
        disableButtons();
        return;
    }
    
    // Marcar o tempo de início da pergunta para calcular bônus de velocidade
    window.questionStartTime = Date.now();
    
    // Verificar se a pergunta tem texto
    if (!currentQuestion.text) {
        console.error('Pergunta sem texto:', currentQuestion);
        addBotMessage('❌ Pergunta inválida. Recarregue a página.');
        disableButtons();
        return;
    }
    
    console.log('Pergunta carregada:', currentQuestion);
    addBotMessage(currentQuestion.text);
    enableButtons();
}

function submitAnswer(userAnswer) {
    // Validação: verificar se há pergunta atual
    if (!currentQuestion) {
        console.error('Erro: currentQuestion é undefined');
        addBotMessage('❌ Erro: Nenhuma pergunta carregada. Recarregue a página.');
        enableButtons();
        return;
    }
    
    disableButtons();
    
    // Add user response
    addUserMessage(userAnswer ? "Verdadeiro!" : "Falso!");
    
    totalQuestions++;
    const questionStartTime = window.questionStartTime || Date.now();
    const timeSpent = Math.floor((Date.now() - questionStartTime) / 1000);
    
    setTimeout(() => {
        let pointsEarned = 0;
        let isCorrect = userAnswer === currentQuestion.answer;
        
        if (isCorrect) {
            correctAnswers++;
            currentStreak++;
            
            if (currentStreak > bestStreak) {
                bestStreak = currentStreak;
            }
            
            // Calcular pontos
            pointsEarned = calculatePoints(timeSpent);
            totalScore += pointsEarned;
            
            // Atualizar multiplicador baseado em streak
            updateMultiplier();
            
            // Verificar level up
            checkLevelUp();
            
            // Mensagem de feedback com pontos
            let message = `✓ Correto! +${pointsEarned} pontos!`;
            if (currentStreak >= 3) {
                message += ` 🔥 ${currentStreak}x Combo!`;
            }
            addBotMessage(message);
            
            // Mostrar efeito visual de pontos
            showPointsAnimation(pointsEarned);
            
            // Mostrar curiosidade
            if (currentQuestion.curiosity) {
                setTimeout(() => {
                    addBotMessage("💡 " + currentQuestion.curiosity);
                }, 800);
            }
        } else {
            // Resposta errada
            currentStreak = 0;
            multiplier = 1;
            pointsEarned = POINTS.WRONG_PENALTY;
            totalScore = Math.max(0, totalScore + pointsEarned); // Não deixar score negativo
            
            addBotMessage(`✗ Ops! Resposta incorreta. A resposta era ${currentQuestion.answer ? "Verdadeiro" : "Falso"}. ${pointsEarned} pontos.`);
            
            // Mostrar curiosidade
            if (currentQuestion.curiosity) {
                setTimeout(() => {
                    addBotMessage("💡 " + currentQuestion.curiosity);
                }, 800);
            }
        }
        
        updateGameStats();
        
        setTimeout(() => {
            if (questionsQueue.length > 0) {
                loadNextQuestion();
            } else {
                endGame();
            }
        }, currentQuestion && currentQuestion.curiosity ? 2500 : 1500);
    }, 800);
}

function calculatePoints(timeSpent) {
    let points = POINTS.CORRECT_BASE;
    
    // Bônus de streak
    if (currentStreak > 0) {
        points += POINTS.STREAK_BONUS * Math.floor(currentStreak / 3);
    }
    
    // Bônus de velocidade (responder rápido)
    if (timeSpent <= 3) {
        points += POINTS.SPEED_BONUS_MAX;
    } else if (timeSpent <= 5) {
        points += Math.floor(POINTS.SPEED_BONUS_MAX * 0.7);
    } else if (timeSpent <= 8) {
        points += Math.floor(POINTS.SPEED_BONUS_MAX * 0.4);
    }
    
    // Aplicar multiplicador
    points = Math.floor(points * multiplier);
    
    return points;
}

function updateMultiplier() {
    if (currentStreak >= 20) {
        multiplier = 3.0;
    } else if (currentStreak >= 15) {
        multiplier = 2.5;
    } else if (currentStreak >= 10) {
        multiplier = 2.0;
    } else if (currentStreak >= 5) {
        multiplier = 1.5;
    } else if (currentStreak >= 3) {
        multiplier = 1.2;
    } else {
        multiplier = 1.0;
    }
}

function checkLevelUp() {
    const newLevel = LEVELS.reverse().find(level => totalScore >= level.minScore) || LEVELS[0];
    LEVELS.reverse(); // Restaurar ordem original
    
    if (newLevel.name !== currentLevel.name) {
        const oldLevel = currentLevel;
        currentLevel = newLevel;
        
        // Animação de level up
        setTimeout(() => {
            addBotMessage(`🎉 LEVEL UP! Você alcançou o nível: ${currentLevel.icon} ${currentLevel.name}!`);
            showLevelUpAnimation();
        }, 1000);
    }
}

function addBotMessage(text) {
    const chatArea = document.getElementById('chatArea');
    const messageDiv = document.createElement('div');
    messageDiv.className = 'message bot-message';
    messageDiv.innerHTML = `
        <div class="bot-icon">
            <div class="bot-face">
                <div class="bot-eyes">
                    <span class="eye"></span>
                    <span class="eye"></span>
                </div>
                <div class="bot-mouth"></div>
            </div>
        </div>
        <div class="message-bubble">
            <p>${text}</p>
        </div>
    `;
    chatArea.appendChild(messageDiv);
    scrollToBottom();
}

function addUserMessage(text) {
    const chatArea = document.getElementById('chatArea');
    const messageDiv = document.createElement('div');
    messageDiv.className = 'message user-message';
    messageDiv.innerHTML = `
        <div class="message-bubble">
            <p>${text}</p>
        </div>
    `;
    chatArea.appendChild(messageDiv);
    scrollToBottom();
}

function scrollToBottom() {
    const chatArea = document.getElementById('chatArea');
    chatArea.scrollTop = chatArea.scrollHeight;
}

function updateGameStats() {
    // Atualizar pontuação e estatísticas
    document.getElementById('scoreValue').textContent = totalScore.toLocaleString();
    document.getElementById('accuracyValue').textContent = `${correctAnswers}/${totalQuestions}`;
    document.getElementById('streakValue').textContent = currentStreak;
    document.getElementById('levelValue').textContent = `${currentLevel.icon} ${currentLevel.name}`;
    
    // Atualizar multiplicador
    const multiplierEl = document.getElementById('multiplierValue');
    if (multiplierEl) {
        multiplierEl.textContent = `${multiplier.toFixed(1)}x`;
        multiplierEl.style.color = multiplier > 1 ? '#38ef7d' : '#fff';
    }
}

function startTimer() {
    if (timerInterval) clearInterval(timerInterval);
    
    timerInterval = setInterval(() => {
        timerSeconds++;
        const minutes = Math.floor(timerSeconds / 60);
        const seconds = timerSeconds % 60;
        document.getElementById('timerValue').textContent = 
            `${minutes}:${seconds.toString().padStart(2, '0')}`;
    }, 1000);
}

function stopTimer() {
    if (timerInterval) {
        clearInterval(timerInterval);
        timerInterval = null;
    }
}

function enableButtons() {
    const buttons = document.querySelectorAll('.answer-buttons .btn');
    console.log(`Habilitando ${buttons.length} botões`);
    buttons.forEach(btn => {
        btn.disabled = false;
        btn.style.opacity = '1';
        btn.style.pointerEvents = 'auto';
    });
}

function disableButtons() {
    const buttons = document.querySelectorAll('.answer-buttons .btn');
    console.log(`Desabilitando ${buttons.length} botões`);
    buttons.forEach(btn => {
        btn.disabled = true;
        btn.style.opacity = '0.5';
        btn.style.pointerEvents = 'none';
    });
}

function endGame() {
    stopTimer();
    disableButtons();
    
    const percentage = totalQuestions > 0 ? Math.round((correctAnswers / totalQuestions) * 100) : 0;
    
    setTimeout(() => {
        addBotMessage("🎮 FIM DE JOGO! 🎮");
        
        setTimeout(() => {
            let statsMessage = `
📊 ESTATÍSTICAS FINAIS:

🏆 Pontuação Total: ${totalScore.toLocaleString()} pontos
✅ Acertos: ${correctAnswers}/${totalQuestions} (${percentage}%)
🔥 Melhor Sequência: ${bestStreak}x combo
${currentLevel.icon} Nível Alcançado: ${currentLevel.name}
⏱️ Tempo Total: ${formatTime(timerSeconds)}
            `.trim();
            
            addBotMessage(statsMessage);
            
            setTimeout(() => {
                let performanceMessage = "";
                
                if (percentage === 100) {
                    performanceMessage = "🏆 PERFEITO! Você é um mestre absoluto do conhecimento!";
                } else if (percentage >= 90) {
                    performanceMessage = "⭐ EXCELENTE! Conhecimento impressionante!";
                } else if (percentage >= 75) {
                    performanceMessage = "🎉 ÓTIMO! Muito bem jogado!";
                } else if (percentage >= 60) {
                    performanceMessage = "👍 BOM! Continue assim!";
                } else if (percentage >= 40) {
                    performanceMessage = "📚 Continue praticando! Você vai melhorar!";
                } else {
                    performanceMessage = "💪 Não desista! A prática leva à perfeição!";
                }
                
                addBotMessage(performanceMessage);
                
                setTimeout(() => {
                    addBotMessage("🔄 Quer jogar novamente? Atualize a página!");
                }, 1500);
            }, 1500);
        }, 1000);
    }, 1000);
}

function formatTime(seconds) {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
}

// Animações visuais
function showPointsAnimation(points) {
    const pointsEl = document.createElement('div');
    pointsEl.className = 'points-animation';
    pointsEl.textContent = `+${points}`;
    pointsEl.style.cssText = `
        position: fixed;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
        font-size: 3rem;
        font-weight: bold;
        color: #38ef7d;
        text-shadow: 0 0 20px rgba(56, 239, 125, 0.8);
        z-index: 9999;
        animation: pointsFloat 1.5s ease-out;
        pointer-events: none;
    `;
    
    document.body.appendChild(pointsEl);
    
    setTimeout(() => {
        pointsEl.remove();
    }, 1500);
}

function showLevelUpAnimation() {
    const levelUpEl = document.createElement('div');
    levelUpEl.className = 'levelup-animation';
    levelUpEl.innerHTML = `
        <div style="
            position: fixed;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            padding: 2rem 3rem;
            border-radius: 20px;
            box-shadow: 0 20px 60px rgba(0,0,0,0.5);
            z-index: 9999;
            animation: levelUpPulse 2s ease-out;
            text-align: center;
        ">
            <div style="font-size: 3rem; margin-bottom: 1rem;">🎊</div>
            <div style="font-size: 2rem; font-weight: bold; color: white;">LEVEL UP!</div>
            <div style="font-size: 1.5rem; color: white; margin-top: 0.5rem;">
                ${currentLevel.icon} ${currentLevel.name}
            </div>
        </div>
    `;
    
    document.body.appendChild(levelUpEl);
    
    setTimeout(() => {
        levelUpEl.remove();
    }, 2000);
}

// Funcionalidade do botão fechar
document.addEventListener('DOMContentLoaded', function() {
    const closeBtn = document.querySelector('.close-btn');
    if (closeBtn) {
        closeBtn.addEventListener('click', function() {
            if (confirm('Tem certeza que deseja sair do jogo?')) {
                stopTimer();
                window.location.href = '/';
            }
        });
    }
});
