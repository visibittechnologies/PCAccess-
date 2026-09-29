$(document).ready(function () {
    // Floating AI Chatbot DOM Elements
    const floatingChatToggle = $('#ai-chat-toggle');
    const floatingChatWindow = $('#ai-chat-window');
    const floatingCloseChat = $('#close-chat');
    const floatingMaximizeChat = $('#maximize-chat');
    const floatingNewChatBtn = $('#new-chat-btn');
    const floatingChatForm = $('#ai-chat-form');
    const floatingChatInput = $('#ai-chat-input');
    const floatingChatMessages = $('#ai-chat-messages');
    const floatingSubmitBtn = floatingChatForm.find('button[type="submit"]');

    let isSubmitting = false;

    // Session-based persistent conversation ID
    function getConversationId() {
        let convId = sessionStorage.getItem('nkosh_ai_conversation_id');
        if (!convId || convId.trim() === '') {
            convId = 'conv_' + Date.now() + '_' + Math.random().toString(36).substring(2, 9);
            sessionStorage.setItem('nkosh_ai_conversation_id', convId);
        }
        return convId;
    }

    // Generate unique client message ID for deduplication
    function generateClientMessageId() {
        return 'msg_' + Date.now() + '_' + Math.random().toString(36).substring(2, 9);
    }

    floatingNewChatBtn.on('click', function () {
        if (isSubmitting) return;

        $.ajax({
            url: '/AiAssistant/NewConversation',
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify({}),
            success: function (res) {
                if (res && res.conversationId) {
                    sessionStorage.setItem('nkosh_ai_conversation_id', res.conversationId);
                }
                floatingChatMessages.empty();
                floatingChatMessages.append(`
                    <div class="flex flex-col gap-1 max-w-[85%] self-start">
                        <div class="bg-white p-3 rounded-2xl rounded-tl-sm shadow-sm text-zinc-700 border border-zinc-100 text-[13px]">
                            Namaste! I have started a fresh conversation for you. How can I help you today?
                        </div>
                        <span class="text-[10px] text-zinc-400 ml-1">Just now</span>
                    </div>
                `);
                floatingChatInput.val('').focus();
            },
            error: function () {
                const newId = 'conv_' + Date.now() + '_' + Math.random().toString(36).substring(2, 9);
                sessionStorage.setItem('nkosh_ai_conversation_id', newId);
                floatingChatMessages.empty();
                floatingChatMessages.append(`
                    <div class="flex flex-col gap-1 max-w-[85%] self-start">
                        <div class="bg-white p-3 rounded-2xl rounded-tl-sm shadow-sm text-zinc-700 border border-zinc-100 text-[13px]">
                            Namaste! Fresh conversation started. How can I help you?
                        </div>
                        <span class="text-[10px] text-zinc-400 ml-1">Just now</span>
                    </div>
                `);
                floatingChatInput.val('').focus();
            }
        });
    });

    floatingChatToggle.on('click', function () {
        floatingChatToggle.removeClass('flex').addClass('hidden');
        floatingChatWindow.removeClass('hidden').addClass('flex');
        setTimeout(() => {
            floatingChatWindow.removeClass('scale-95 opacity-0').addClass('scale-100 opacity-100');
            floatingChatInput.focus();
        }, 10);
    });

    floatingMaximizeChat.on('click', function () {
        floatingChatWindow.toggleClass('maximized');
        const iconSpan = floatingMaximizeChat.find('span');
        if (floatingChatWindow.hasClass('maximized')) {
            iconSpan.text('fullscreen_exit');
            floatingChatWindow.css({
                'width': '90vw',
                'height': '90vh',
                'max-width': '1200px',
                'max-height': '800px',
                'bottom': '5vh',
                'right': '5vw'
            });
        } else {
            iconSpan.text('fullscreen');
            floatingChatWindow.css({
                'width': '',
                'height': '',
                'max-width': '',
                'max-height': '',
                'bottom': '',
                'right': ''
            });
        }
    });

    floatingCloseChat.on('click', function () {
        floatingChatWindow.removeClass('scale-100 opacity-100').addClass('scale-95 opacity-0');
        setTimeout(() => {
            floatingChatWindow.removeClass('flex').addClass('hidden');
            floatingChatWindow.removeClass('maximized');
            floatingChatWindow.css({
                'width': '',
                'height': '',
                'max-width': '',
                'max-height': '',
                'bottom': '',
                'right': ''
            });
            floatingMaximizeChat.find('span').text('fullscreen');
            floatingChatToggle.removeClass('hidden').addClass('flex');
        }, 300);
    });

    function startChatSession() {
        const startState = $('#ai-chat-start-state');
        const msgLog = $('#ai-chat-messages');
        const mascotPeek = $('#ai-chat-mascot-peek');

        if (startState.is(':visible')) {
            startState.hide();
            msgLog.removeClass('hidden').addClass('flex flex-col');
            mascotPeek.addClass('translate-y-full opacity-0');
        }
    }

    function escapeHtml(unsafe) {
        if (unsafe === undefined || unsafe === null) return '';
        return String(unsafe)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    // Strictly validate URLs to allow only safe internal relative paths
    function sanitizeUrl(url) {
        if (!url || typeof url !== 'string') return '/Home/Index';
        const trimmed = url.trim();
        // Allow relative routes starting with /Home/ or safe hash
        if (trimmed.startsWith('/Home/') || trimmed.startsWith('#')) {
            return encodeURI(trimmed);
        }
        if (trimmed.startsWith('/') && !trimmed.startsWith('//')) {
            return encodeURI(trimmed);
        }
        return '/Home/Index';
    }

    function formatMarkdownText(text) {
        if (!text) return '';
        let escaped = escapeHtml(text);
        // Replace **bold** with <strong>bold</strong>
        escaped = escaped.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');
        // Replace *bullet with list styling
        escaped = escaped.replace(/^• (.*?)$/gm, '<div class="flex items-start gap-1.5 my-1"><span class="text-[#1A7A4A] font-bold">•</span><span>$1</span></div>');
        // Replace newlines with <br/>
        escaped = escaped.replace(/\n/g, '<br/>');
        return escaped;
    }

    // Render Rich Blog Cards
    function renderBlogCardsHtml(blogs) {
        if (!blogs || !Array.isArray(blogs) || blogs.length === 0) return '';

        let html = '<div class="mt-3 flex flex-col gap-2.5">';
        blogs.forEach(function (blog) {
            const title = escapeHtml(blog.Title || blog.title || 'PCAccess');
            const category = escapeHtml(blog.Category || blog.category || 'Article');
            const summary = escapeHtml(blog.Summary || blog.summary || '');
            const rawUrl = blog.Url || blog.url || ('/Home/BlogDetails?id=' + (blog.BlogId || blog.blogId || ''));
            const safeUrl = sanitizeUrl(rawUrl);
            const dateStr = escapeHtml(blog.PublishDate || blog.publishDate || '');

            html += `
                <div class="bg-[#FAFDF9] border border-[#D5EADB] rounded-xl p-3 shadow-xs hover:border-[#1A7A4A]/50 hover:shadow-sm transition-all duration-200">
                    <div class="flex items-center justify-between gap-2 mb-1.5">
                        <span class="inline-block bg-[#E7F6EC] text-[#1A7A4A] text-[10px] font-bold uppercase tracking-wider px-2 py-0.5 rounded-full">
                            ${category}
                        </span>
                        ${dateStr ? `<span class="text-[10px] text-zinc-400 font-medium">${dateStr}</span>` : ''}
                    </div>
                    <h4 class="font-bold text-[12px] text-zinc-900 leading-snug mb-1">
                        ${title}
                    </h4>
                    ${summary ? `<p class="text-[11px] text-zinc-600 line-clamp-2 leading-relaxed mb-2">${summary}</p>` : ''}
                    <div class="pt-1.5 border-t border-[#E8EFEA] flex justify-end">
                        <a href="${safeUrl}" class="inline-flex items-center gap-1 text-[11px] font-bold text-[#1A7A4A] hover:text-[#114900] hover:underline transition-colors">
                            <span>Read Article</span>
                            <span class="material-symbols-outlined text-[13px]">arrow_forward</span>
                        </a>
                    </div>
                </div>
            `;
        });
        html += '</div>';
        return html;
    }

    // Render Navigation Action Pills
    function renderActionsHtml(actions) {
        if (!actions || !Array.isArray(actions) || actions.length === 0) return '';

        let actionsHtml = '<div class="mt-3 pt-2.5 border-t border-zinc-100 flex flex-wrap gap-2">';
        actions.forEach(act => {
            const safeUrl = sanitizeUrl(act.url || act.Url);
            const label = escapeHtml(act.label || act.Label || 'View Details');
            actionsHtml += `
                <a href="${safeUrl}" class="inline-flex items-center gap-1.5 px-3 py-1.5 bg-[#FFF9EB] hover:bg-[#ffcd1e]/30 border border-[#ffcd1e] text-[#114900] text-xs font-semibold rounded-xl transition-all shadow-sm">
                    <span>${label}</span>
                    <span class="material-symbols-outlined text-[14px]">arrow_forward</span>
                </a>`;
        });
        actionsHtml += '</div>';
        return actionsHtml;
    }

    function appendFloatingChatMessage(text, isUser, actions, data, responseType, failedQuery) {
        const time = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        let msgHtml = '';

        if (isUser) {
            msgHtml = `
                <div class="flex flex-col gap-1 max-w-[85%] self-end items-end">
                    <div class="bg-gradient-to-r from-[#1A7A4A] to-[#1f6106] text-white p-3 rounded-2xl rounded-tr-sm shadow-sm border border-transparent text-[13px]">
                        ${escapeHtml(text)}
                    </div>
                    <span class="text-[10px] text-zinc-400 mr-1">${time}</span>
                </div>`;
        } else {
            let richCardsHtml = '';
            if (responseType === 'blog_list' || responseType === 'blog' || (data && Array.isArray(data) && data.length > 0)) {
                richCardsHtml = renderBlogCardsHtml(data);
            }

            const actionsHtml = renderActionsHtml(actions);
            const formattedText = formatMarkdownText(text);

            let retryHtml = '';
            if (failedQuery) {
                retryHtml = `
                    <div class="mt-2.5 pt-2 border-t border-red-100 flex items-center justify-between">
                        <span class="text-[11px] text-red-500 font-medium flex items-center gap-1">
                            <span class="material-symbols-outlined text-[13px]">error</span>
                            Request failed
                        </span>
                        <button type="button" class="chat-retry-btn inline-flex items-center gap-1 px-2.5 py-1 bg-red-50 hover:bg-red-100 text-red-700 text-[11px] font-semibold rounded-lg border border-red-200 transition-all cursor-pointer" data-retry="${escapeHtml(failedQuery)}">
                            <span class="material-symbols-outlined text-[12px]">refresh</span>
                            <span>Try Again</span>
                        </button>
                    </div>`;
            }

            msgHtml = `
                <div class="flex flex-col gap-1 max-w-[88%] self-start">
                    <div class="bg-white p-3.5 rounded-2xl rounded-tl-sm shadow-sm text-zinc-800 border border-zinc-100 leading-relaxed text-[13px]">
                        ${formattedText}
                        ${richCardsHtml}
                        ${actionsHtml}
                        ${retryHtml}
                    </div>
                    <span class="text-[10px] text-zinc-400 ml-1">${time}</span>
                </div>`;
        }

        floatingChatMessages.append(msgHtml);
        floatingChatMessages.scrollTop(floatingChatMessages[0].scrollHeight);
    }

    function showFloatingChatTyping() {
        const typingHtml = `
            <div id="ai-chat-typing" class="flex flex-col gap-1 max-w-[85%] self-start">
                <div class="bg-white p-3.5 rounded-2xl rounded-tl-sm shadow-sm flex items-center gap-2 border border-zinc-100">
                    <span class="text-[11px] text-zinc-400 font-medium">Kisaan AI is thinking</span>
                    <div class="flex items-center gap-1">
                        <div class="w-1.5 h-1.5 bg-[#1A7A4A] rounded-full animate-bounce"></div>
                        <div class="w-1.5 h-1.5 bg-[#1A7A4A] rounded-full animate-bounce" style="animation-delay: 0.15s"></div>
                        <div class="w-1.5 h-1.5 bg-[#1A7A4A] rounded-full animate-bounce" style="animation-delay: 0.3s"></div>
                    </div>
                </div>
            </div>`;
        floatingChatMessages.append(typingHtml);
        floatingChatMessages.scrollTop(floatingChatMessages[0].scrollHeight);
    }

    function removeFloatingChatTyping() {
        $('#ai-chat-typing').remove();
    }

    function setChatLoading(isLoading) {
        isSubmitting = isLoading;
        floatingChatInput.prop('disabled', isLoading);
        floatingSubmitBtn.prop('disabled', isLoading);

        if (isLoading) {
            floatingSubmitBtn.addClass('opacity-70 cursor-not-allowed');
            floatingSubmitBtn.find('span').text('Thinking...');
            floatingSubmitBtn.find('i').removeClass('fa-circle-nodes').addClass('fa-spinner fa-spin');
            showFloatingChatTyping();
        } else {
            floatingSubmitBtn.removeClass('opacity-70 cursor-not-allowed');
            floatingSubmitBtn.find('span').text('Talk');
            floatingSubmitBtn.find('i').removeClass('fa-spinner fa-spin').addClass('fa-circle-nodes');
            removeFloatingChatTyping();
            floatingChatInput.focus();
        }
    }

    function sendChatQuery(query) {
        if (!query || isSubmitting) return;

        const currentConvId = getConversationId();
        const clientMsgId = generateClientMessageId();

        setChatLoading(true);

        $.ajax({
            url: '/AiAssistant/SendMessage',
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify({
                message: query,
                conversationId: currentConvId,
                clientMessageId: clientMsgId
            }),
            timeout: 30000,
            success: function (res) {
                setChatLoading(false);
                if (res && res.success) {
                    if (res.conversationId) {
                        sessionStorage.setItem('nkosh_ai_conversation_id', res.conversationId);
                    }
                    appendFloatingChatMessage(res.message, false, res.actions, res.data, res.responseType);
                } else {
                    const fallbackMsg = (res && res.message) ? res.message : "I am having trouble retrieving that information right now. Please try again.";
                    appendFloatingChatMessage(fallbackMsg, false, null, null, null, query);
                }
            },
            error: function (xhr, status, error) {
                setChatLoading(false);
                const errMsg = "Unable to connect to Kisaan AI right now. Please check your network connection and try again.";
                appendFloatingChatMessage(errMsg, false, null, null, null, query);
            }
        });
    }

    // Chat Suggestion Buttons
    $(document).on('click', '.chat-suggestion-btn', function () {
        if (isSubmitting) return;
        const question = $(this).attr('data-question');
        if (!question) return;

        startChatSession();
        appendFloatingChatMessage(question, true);
        sendChatQuery(question);
    });

    // Retry Button Click
    $(document).on('click', '.chat-retry-btn', function () {
        if (isSubmitting) return;
        const retryQuery = $(this).attr('data-retry');
        if (!retryQuery) return;

        $(this).closest('.chat-retry-btn').prop('disabled', true).addClass('opacity-50');
        appendFloatingChatMessage(retryQuery, true);
        sendChatQuery(retryQuery);
    });

    // Form Submit
    floatingChatForm.on('submit', function (e) {
        e.preventDefault();
        if (isSubmitting) return;

        const text = floatingChatInput.val().trim();
        if (!text) return;

        startChatSession();
        appendFloatingChatMessage(text, true);
        floatingChatInput.val('');
        sendChatQuery(text);
    });

    // Handle Enter key on input (prevent default form submission doubling)
    floatingChatInput.on('keydown', function (e) {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            floatingChatForm.submit();
        }
    });
});

