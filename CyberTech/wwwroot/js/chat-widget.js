// chat-widget.js
let isChatOpen = false;
let chatHistory = JSON.parse(localStorage.getItem('chatHistory')) || [];
let unreadMessages = 0;

document.addEventListener('DOMContentLoaded', () => {
    const chatWidget = document.getElementById('chat-widget');
    const chatBubble = document.getElementById('chat-bubble');
    const chatInput = document.getElementById('chat-input');
    const sendButton = document.getElementById('send-message');
    const minimizeButton = document.getElementById('chat-minimize');
    const maximizeButton = document.getElementById('chat-maximize');
    const closeButton = document.getElementById('chat-close');
    const messagesContainer = document.getElementById('chat-messages');
    const scrollBottomBtn = document.getElementById('scroll-bottom-btn');
    
    let isMaximized = false;

    scrollBottomBtn.innerHTML = '<i class="fas fa-chevron-down"></i>';

    function loadChatHistory() {
        const savedHistory = localStorage.getItem('chatHistory');
        if (savedHistory) {
            chatHistory = JSON.parse(savedHistory);
            chatHistory.forEach(msg => {
                appendMessage(msg.content, msg.role === 'user' ? 'user-message' : 'bot-message', msg.role !== 'user');
            });
        }
        scrollToBottom();
    }

    function saveChatHistory() {
        // Limit history to the last 20 messages to avoid performance issues
        if (chatHistory.length > 20) {
            chatHistory = chatHistory.slice(-20);
        }
        localStorage.setItem('chatHistory', JSON.stringify(chatHistory));
    }

    function toggleChat() {
        isChatOpen = !isChatOpen;
        chatWidget.classList.toggle('visible', isChatOpen);
        chatBubble.classList.toggle('hidden', isChatOpen);
        if (isChatOpen) {
            chatInput.focus();
            scrollToBottom();
        }
    }

    function scrollToBottom() {
        messagesContainer.scrollTo({
            top: messagesContainer.scrollHeight,
            behavior: 'smooth'
        });
        unreadMessages = 0;
        updateScrollButtonBadge();
        updateScrollButtonVisibility();
    }

    function updateScrollButtonVisibility() {
        const isNearBottom = messagesContainer.scrollHeight - messagesContainer.scrollTop - messagesContainer.clientHeight < 100;
        const hasContent = messagesContainer.scrollHeight > messagesContainer.clientHeight;
        scrollBottomBtn.classList.toggle('visible', !isNearBottom && hasContent);
    }

    function updateScrollButtonBadge() {
        const badge = scrollBottomBtn.querySelector('.unread-badge') || document.createElement('span');
        badge.className = 'unread-badge';
        badge.textContent = unreadMessages > 0 ? unreadMessages : '';
        if (!scrollBottomBtn.contains(badge)) {
            scrollBottomBtn.appendChild(badge);
        }
    }

    messagesContainer.addEventListener('scroll', () => {
        updateScrollButtonVisibility();
        const isNearBottom = messagesContainer.scrollHeight - messagesContainer.scrollTop - messagesContainer.clientHeight < 100;
        if (isNearBottom) {
            unreadMessages = 0;
            updateScrollButtonBadge();
        }
    });

    scrollBottomBtn.addEventListener('click', scrollToBottom);

    chatBubble.addEventListener('click', toggleChat);
    closeButton.addEventListener('click', toggleChat);
    minimizeButton.addEventListener('click', toggleChat);

    // Click outside to close chat widget
    document.addEventListener('click', function(e) {
        if (isChatOpen && !chatWidget.contains(e.target) && !chatBubble.contains(e.target)) {
            toggleChat();
        }
    });

    maximizeButton.addEventListener('click', () => {
        isMaximized = !isMaximized;
        chatWidget.classList.toggle('maximized');
        maximizeButton.innerHTML = isMaximized ? 
            '<i class="fas fa-compress"></i>' : 
            '<i class="fas fa-expand"></i>';
    });

    async function sendMessage() {
        const message = chatInput.value.trim();
        if (!message) return;

        appendMessage(message, 'user-message');
        chatHistory.push({ role: 'user', content: message });
        saveChatHistory();

        chatInput.value = '';
        chatInput.disabled = true;
        sendButton.disabled = true;

        showTypingIndicator(true);
        scrollToBottom();

        try {
            // Get the antiforgery token - search in multiple places to ensure we find it
            const token = getAntiForgeryToken();
            
            if (!token) {
                console.error('Antiforgery token not found');
                throw new Error('Không tìm thấy token bảo mật. Vui lòng tải lại trang.');
            }

            // Prepare conversation history for the request (last 10 messages to avoid token limits)
            const conversationHistory = chatHistory.slice(-10).map(msg => ({
                role: msg.role,
                content: msg.content
            }));

            const response = await fetch('/api/Chat/GeminiChat', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({
                    userInput: message,
                    conversationHistory: conversationHistory
                })
            });

            if (!response.ok) {
                const errorText = await response.text();
                console.error('Chat API Error:', {
                    status: response.status,
                    statusText: response.statusText,
                    responseText: errorText
                });
                throw new Error(`Lỗi kết nối: ${response.status}`);
            }

            const result = await response.json();

            showTypingIndicator(false);
            chatInput.disabled = false;
            sendButton.disabled = false;
            chatInput.focus();

            if (result.success) {
                appendMessage(result.html, 'bot-message', true);
                chatHistory.push({ role: 'bot', content: result.html });
                saveChatHistory();
                scrollToBottom();
            } else {
                throw new Error(result.message || 'Không thể xử lý yêu cầu');
            }

        } catch (error) {
            console.error('Chat error:', error);
            showTypingIndicator(false);
            chatInput.disabled = false;
            sendButton.disabled = false;
            appendMessage(`Xin lỗi, hiện tại tôi không thể trả lời: ${error.message}`, 'bot-message error');
            chatHistory.push({ role: 'bot', content: 'Xin lỗi, hiện tại tôi không thể trả lời. Vui lòng thử lại sau.' });
            saveChatHistory();
            scrollToBottom();
        }
    }

    // Function to get the antiforgery token from various possible locations
    function getAntiForgeryToken() {
        // Try to find the token in various places
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value ||
               document.querySelector('form input[name="__RequestVerificationToken"]')?.value ||
               document.querySelector('meta[name="__RequestVerificationToken"]')?.content;
    }

    function appendMessage(content, className, isHtml = false) {
        const messageDiv = document.createElement('div');
        messageDiv.className = `message ${className}`;
        
        const contentDiv = document.createElement('div');
        contentDiv.className = 'message-content';
        if (isHtml) {
            contentDiv.innerHTML = content;
            const images = contentDiv.querySelectorAll('img');
            images.forEach(img => {
                img.onerror = () => {
                    img.src = '/images/no-image.png';
                    img.alt = 'Image not available';
                };
            });
            const links = contentDiv.querySelectorAll('a');
            links.forEach(link => {
                link.addEventListener('click', () => {
                    console.log('User clicked product link:', link.href);
                });
            });
        } else {
            contentDiv.textContent = content;
        }
        
        const timestampDiv = document.createElement('div');
        timestampDiv.className = 'message-timestamp';
        timestampDiv.textContent = new Date().toLocaleTimeString();
        
        messageDiv.appendChild(contentDiv);
        messageDiv.appendChild(timestampDiv);
        messagesContainer.appendChild(messageDiv);
        
        const isNearBottom = messagesContainer.scrollHeight - messagesContainer.scrollTop - messagesContainer.clientHeight < 100;
        if (!isNearBottom) {
            unreadMessages++;
            updateScrollButtonBadge();
        }
        scrollToBottom();
    }

    function showTypingIndicator(show) {
        const typingIndicator = document.getElementById('typing-indicator');
        typingIndicator.classList.toggle('visible', show);
    }

    sendButton.addEventListener('click', sendMessage);
    
    chatInput.addEventListener('keypress', (e) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            sendMessage();
        }
    });

    chatInput.addEventListener('input', () => {
        chatInput.style.height = 'auto';
        chatInput.style.height = Math.min(chatInput.scrollHeight, 100) + 'px';
    });

    loadChatHistory();
});