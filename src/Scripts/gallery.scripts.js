var gallery = this.gallery || {};

// variables
gallery.filter = '';
gallery.sort = '';

// toggle vote
gallery.vote = function (anchor) {
   var id = $(anchor).parent().attr('data-id');
   var info = $("div.info[data-id='" + id + "']");
   var star = info.find('.star');
   var counter = info.find('.counter');

   // saving already in progress
   if (info.is('.saving')) return;

   info.addClass('saving').fadeTo('slow', 0.75);
   star.effect("pulsate", { times: 10 }, 750);

   $.ajax({
      url: $(anchor).attr('href'),
      type: 'GET',
      dataType: 'json',
      cache: false,
      success: function (data) {
         star.toggleClass('voted', data.UserVote);
         counter
            .attr("title", data.TotalVotes + ' person(s) voted this photo')
            .toggleClass('no-votes', data.TotalVotes == 0)
            .find('span').html(data.TotalVotes);

      },
      complete: function () {
         star.stop(true).fadeTo('slow', 1.0);
         info.stop(true).fadeTo('slow', 1.0).removeClass('saving');
      }
   });
};

// remove image
gallery.remove = function (anchor) {
   var del = $(anchor);

   // saving already in progress
   if (del.is('.deleting')) return;

   del.addClass('deleting').fadeTo('slow', 0.75);
   del.effect("pulsate", { times: 10 }, 750);

   $.ajax({
      url: $(anchor).attr('href'),
      type: 'POST',
      dataType: 'json',
      cache: false,
      success: function (data) {
         del.closest('li').hide('slow').remove();
      },
      error: function (jqXHR, textStatus, errorThrown) {
         alert(errorThrown);
      },
      complete: function () {
         del.stop(true).fadeTo('slow', 1.0);
         del.stop(true).fadeTo('slow', 1.0).removeClass('deleting');
      }
   });
};

// hook events
gallery.registerFancybox = function () {
   $("a[rel='appendix']").fancybox({
      'overlayShow': true,
      'transitionIn': 'elastic',
      'transitionOut': 'elastic',
      'margin': 20,
      'overlayColor': '#080808',
      'titlePosition': 'over',
      'scrolling': 'no',
      'onComplete': function (links, index) {
         var infoBox = $(links[index]).parent().children("div.info").clone();
         infoBox.css('display', 'none').appendTo("#fancybox-outer").fadeIn(400, 'swing');
      },
      'onStart': function (links, index) {
         $("#fancybox-outer div.info").remove();
      }
   });
};

gallery.registerContactForm = function () {
   // hook up the contact form
   $(".openDialog").live("click", function (e) {
      e.preventDefault();
      $.ajax({
         url: this.href,
         type: 'GET',
         cache: false,
         success: function (data) {
            $.fancybox(data,
                  {
                     'scrolling': 'no',
                     'titleShow': false,
                     'autoDimensions': true,
                     'width': 510,
                     'height': 'auto',
                     'overlayShow': true,
                     'transitionIn': 'elastic',
                     'transitionOut': 'elastic',
                     'onStart': function (links, index) { $("#fancybox-outer div.info").remove(); }
                  });
         }
      });
   });
};

// document ready
$(function () {
   // fancybox for images
   gallery.registerFancybox();

   // fancybox for contact form
   gallery.registerContactForm();

   // when an photo is hovered
   $('#photos img').live({
      mouseover: function () {
         $(this).fadeTo(300, 0.75, "swing");
      },
      mouseout: function () {
         $(this).fadeTo(300, 1, "swing");
      }
   });

   // when vote photo is clicked
   $('a.star').live('click', function (e) {
      e.preventDefault();
      gallery.vote(this);
   });

   // when remove photo is clicked
   $('a.delete').live('click', function (e) {
      e.preventDefault();
      gallery.remove(this);
   });

   //topmenu
   $('ul.splitter a').click(function (e) {
      var anchor = $(this);

      if (anchor.is('[data-filter]')) {
         gallery.filter = anchor.attr('data-filter');
      }
      else if (anchor.is('[data-sort]')) {
         gallery.sort = anchor.attr('data-sort');
      }
      else {
         return;
      }

      $.ajax({
         url: anchor.attr('href') + '?filter=' + gallery.filter + '&sort=' + gallery.sort,
         data: { async: "true" },
         type: 'GET',
         success: function (data) {
            $('#photos').quicksand($(data).find('li.photo'), { adjustHeight: 'dynamic', duration: 800, easing: 'easeInOutQuad' }, gallery.registerFancybox);
            anchor.parent().addClass('selected').siblings().removeClass('selected');
         }
      });
      e.preventDefault();
   });

   $('a.new-gallery').live('click', function () {
      $('li#new-gallery').addClass('active');
   });

   $('li#new-gallery .cancel').live('click', function () {
      $('li#new-gallery').removeClass('active');
   });

   // handle login redirects durring ajax calls
   $('body').ajaxComplete(function (e, xhr) {
      //debugger;
      if (xhr.status === 530) { // access denied, login required
         e.preventDefault();
         document.location.href = xhr.getResponseHeader('X-Redirect-To');
      }
   });
});
