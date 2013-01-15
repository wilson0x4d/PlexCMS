(function () {
    // TODO: modularize models into separate files, do not require host site to bundle them.

    PlexLayoutEditorViewModel = function () {
        var self = this;
        this.layouts = ko.observableArray([]);
        this.selectedLayout = ko.observable();
        this.selectLayout = function (layout) {
            $('.layout-editor').parent().children().hide();
            self.selectedLayout(layout);
            $('.layout-editor').show();
        };
        /* sections */
        this.sectionIdForAdd = ko.observable();
        this.sectionAdd = function (layout) {
            $.post('/api/layout/SectionAdd',
                {
                    id: sectionIdForAdd(),
                    layoutId: layout.id()
                },
                function (data, status, xhr) {
                    if (status === 'success') {
                        layout.sections.push({
                            id: ko.observable(self.sectionIdForAdd()),
                            layoutId: ko.observable(layout.id()),
                            ordinal: ko.observable(layout.sections().length),
                            modules: ko.observableArray([])
                        });
                        self.sectionIdForAdd('');
                    }
                }, 'json');
        };
        this.sectionUp = function (section) {
            var layout = self.layouts().filter(function (layout) {
                return layout.id() === section.layoutId();
            })[0];
            var prior = layout.sections()[section.ordinal() - 1];
            if (prior !== undefined) {
                $.post('/api/layout/SectionUp',
                    {
                        id: section.id(),
                        layoutId: section.layoutId()
                    },
                    function (data, status, xhr) {
                        if (status === 'success') {
                            prior.ordinal(section.ordinal());
                            section.ordinal(section.ordinal() - 1);
                        }
                    }, 'json');
            }
        };
        this.sectionDown = function (section) {
            var layout = self.layouts().filter(function (layout) {
                return layout.id() === section.layoutId();
            })[0];
            var next = layout.sections()[section.ordinal() + 1];
            if (next !== undefined) {
                $.post('/api/layout/SectionDown',
                    {
                        id: section.id(),
                        layoutId: section.layoutId()
                    },
                    function (data, status, xhr) {
                        if (status === 'success') {
                            next.ordinal(section.ordinal());
                            section.ordinal(section.ordinal() + 1);
                        }
                    }, 'json');
            }
        };
        this.sectionRemove = function (section) {
            $.post('/api/layout/SectionRemove',
                {
                    id: section.id(),
                    layoutId: section.layoutId()
                },
                function (data, status, xhr) {
                    if (status === 'success') {
                        var layout = self.layouts().filter(function (layout) {
                            return layout.id() === section.layoutId();
                        })[0];
                        layout.sections.remove(section);
                        var sections = layout.sections();
                        for (var i = 0; i < sections.length; i++) {
                            sections[i].ordinal(i);
                        }
                    }
                }, 'json');
        };
        /* modules */
        this.selectedModule = ko.observable();
        this.moduleAdd = function (section) {
            var layout = self.layouts().filter(function (layout) {
                return layout.id() === section.layoutId();
            })[0];
            $.post('/api/layout/ModuleAdd',
                {
                    id: self.selectedModule().id(),
                    layoutId: section.layoutId(),
                    sectionId: section.id(),
                    ordinal: -1
                },
                function (data, status, xhr) {
                    var module = data;
                    if (status === 'success') {
                        section.modules.push({
                            id: ko.observable(data.id),
                            layoutId: ko.observable(data.layoutId),
                            sectionId: ko.observable(data.sectionId),
                            ordinal: ko.observable(section.modules().length)
                        });
                    }
                }, 'json');
        };
        this.moduleUp = function (module) {
            $.post('/api/layout/ModuleUp',
                ko.toJS(module),
                function (data, status, xhr) {
                    if (status === 'success') {
                        var layout = self.layouts().filter(function (l) {
                            return l.id() === module.layoutId();
                        })[0];
                        var section = layout.sections().filter(function (s) {
                            return s.id() === module.sectionId();
                        })[0];
                        var prior = section.modules()[module.ordinal() - 1];
                        if (prior !== undefined) {
                            var ord = module.ordinal();
                            prior.ordinal(ord);
                            module.ordinal(ord - 1);
                        }
                    }
                }, 'json');
        };
        this.moduleDown = function (module) {
            $.post('/api/layout/ModuleDown',
                ko.toJS(module),
                function (data, status, xhr) {
                    if (status === 'success') {
                        var layout = self.layouts().filter(function (l) {
                            return l.id() === module.layoutId();
                        })[0];
                        var section = layout.sections().filter(function (s) {
                            return s.id() === module.sectionId();
                        })[0];
                        var next = section.modules()[module.ordinal() + 1];
                        if (next !== undefined) {
                            var ord = next.ordinal();
                            next.ordinal(ord-1);
                            module.ordinal(ord);
                        }
                    }
                }, 'json');
        };
        this.moduleRemove = function (module) {
            $.post('/api/layout/ModuleRemove',
                ko.toJS(module),
                function (data, status, xhr) {
                    if (status === 'success') {
                        var layout = self.layouts().filter(function (l) {
                            return l.id() === module.layoutId();
                        })[0];
                        var section = layout.sections().filter(function (s) {
                            return s.id() === module.sectionId();
                        })[0];
                        section.modules.remove(module);
                    }
                }, 'json');
        };
        return this;
    };

    PlexPageEditorViewModel = function () {
        var self = this;
        this.layouts = ko.observableArray([]);
        this.selectedLayout = ko.observable();
        this.controllers = ko.observableArray([]);
        this.pages = ko.observableArray([]);
        this.selectedPage = ko.observable();
        this.selectPage = function (page) {
            $('.page-editor').parent().children().hide();
            self.selectedPage(page);
            $('.page-editor').show();
        };
        this.createPage = function (page) {
        };
        this.readPage = function (page) {
            var url = '/' + page.controllerId() + '/' + page.id();
            window.open(url, 'page-viewer', '');
        };
        this.updatePage = function (pages) {
            var pageOld = pages[0];
            var pageNew = pages[1];
            // TODO: send old and new info to server, have server respond with final info
        };
        this.deletePage = function (page) {
            // TODO: confirmation dialog
            $.post('/api/page/pageRemove',
                {
                    controllerId: page.controllerId(),
                    id: page.id()
                },
                function (data, status, xhr) {
                    if (status === 'success') {
                        self.controllers().filter(function (c) {
                            if (c.id() === page.controllerId()) {
                                c.pages().filter(function (p) {
                                    if (p.id() === page.id()) {
                                        page = p;
                                    }
                                    return false;
                                });
                                c.pages.remove(page);
                                $(".page-editor").hide();
                            }
                            return false;
                        });
                    }
                }, 'json');
        };
        /* page title */
        this.titleSet = function (page) {
            $.post('/api/page/TitleSet',
                {
                    id: page.id(),
                    controllerId: page.controllerId(),
                    title: page.title()
                },
                function (data, status, xhr) {
                    if (status === 'success') {
                        // NOP
                    }
                }, 'json');
        };
        /* page layout */
        this.layoutSet = function (page) {
            $.post('/api/page/LayoutSet',
                {
                    id: page.id(),
                    controllerId: page.controllerId(),
                    layoutId: page.layoutId()
                },
                function (data, status, xhr) {
                    if (status === 'success') {
                        // NOP
                    }
                }, 'json');
        };
        /* sections */
        this.sectionAdd = function (section) {
            $.post('/api/page/SectionAdd',
                ko.toJS(section),
                function (data, status, xhr) {
                    if (status === 'success') {
                        section.isPresent(true);
                    }
                }, 'json');
        };
        this.sectionRemove = function (section) {
            $.post('/api/page/SectionRemove',
                ko.toJS(section),
                function (data, status, xhr) {
                    if (status === 'success') {
                        section.isPresent(false);
                        section.modules([]);
                    }
                }, 'json');
        };
        /* modules */
        this.selectedModule = ko.observable();
        this.moduleAdd = function (section) {
            $.post('/api/page/ModuleAdd',
                {
                    id: self.selectedModule().id(),
                    controllerId: section.controllerId(),
                    pageId: section.pageId(),
                    sectionId: section.id(),
                    ordinal: -1
                },
                function (data, status, xhr) {
                    var module = data;
                    if (status === 'success') {
                        section.modules.push({
                            id: ko.observable(data.id),
                            controllerId: ko.observable(data.controllerId),
                            pageId: ko.observable(data.pageId),
                            sectionId: ko.observable(data.sectionId),
                            ordinal: ko.observable(section.modules().length)
                        });
                    }
                }, 'json');
        };
        this.moduleUp = function (module) {
            $.post('/api/page/ModuleUp',
                ko.toJS(module),
                function (data, status, xhr) {
                    if (status === 'success') {
                        var page = self.pages().filter(function (p) {
                            return p.controllerId() === module.controllerId()
                                && p.id() === module.pageId();
                        })[0];
                        var section = page.sections().filter(function (s) {
                            return s.id() === module.sectionId();
                        })[0];
                        var prior = section.modules()[module.ordinal() - 1];
                        if (prior !== undefined) {
                            var ord = module.ordinal();
                            prior.ordinal(ord);
                            module.ordinal(ord - 1);
                        }
                    }
                }, 'json');
        };
        this.moduleDown = function (module) {
            $.post('/api/page/ModuleDown',
                ko.toJS(module),
                function (data, status, xhr) {
                    if (status === 'success') {
                        var page = self.pages().filter(function (p) {
                            return p.controllerId() === module.controllerId()
                                && p.id() === module.pageId();
                        })[0];
                        var section = page.sections().filter(function (s) {
                            return s.id() === module.sectionId();
                        })[0];
                        var next = section.modules()[module.ordinal() + 1];
                        if (next !== undefined) {
                            var ord = next.ordinal();
                            next.ordinal(ord - 1);
                            module.ordinal(ord);
                        }
                    }
                }, 'json');
        };
        this.moduleRemove = function (module) {
            $.post('/api/page/ModuleRemove',
                ko.toJS(module),
                function (data, status, xhr) {
                    if (status === 'success') {
                        var page = self.pages().filter(function (p) {
                            return p.controllerId() === module.controllerId()
                                && p.id() === module.pageId();
                        })[0];
                        var section = page.sections().filter(function (s) {
                            return s.id() === module.sectionId();
                        })[0];
                        section.modules.remove(module);
                    }
                }, 'json');
        };
        /* page body */
        this.bodySet = function (page) {
            $.post('/api/page/BodySet',
                ko.toJS(page),
                function (data, status, xhr) {
                    if (status === 'success') {
                        // NOP
                    }
                }, 'json');
        };
        return this;
    };

    PlexNewPageDialogViewModel = function () {
        var self = this;
        /* props */
        this.controllerId = ko.observable();
        this.pageId = ko.observable();
        this.title = ko.observable();        
        /* methods */
        this.showDialog = function () {
            $('#new-page-dialog').parent().children().hide();
            $('#new-page-dialog').show();
        };
        this.accept = function () {
            $.post('/api/page/upsert',
                {
                    id: self.pageId(),
                    controllerId: self.controllerId(),
                    title: self.title()
                },
                function (data, status, xhr) {
                    if (status === 'success') {
                        self.onPageUpsert(data);
                        $('#new-page-dialog').hide();
                    }
                }, 'json');
        };
        this.cancel = function () {
            $('#new-page-dialog').hide();
        };
        // raised on page upsert success
        this.onPageUpsert = function (page) {
        };
        return this;
    };

    PlexModuleEditorViewModel = function () {
        var self = this;
        this.modules = ko.observableArray([]);
        this.selectedModule = ko.observable();
        this.selectModule = function (module) {
            $('.module-editor').parent().children().hide();
            self.selectedModule(module);
            $('.module-editor').show();
        };
        this.moduleUpsert = function (module) {
            $.post('/api/module/upsert',
                ko.toJS(module),
                function (data, status, xhr) {
                    if (status === 'success') {
                        alert('success'); // TODO
                    } else {
                        alert('fail'); // TODO
                    }
                }, 'json');
        };
        this.moduleRemove = function (module) {
            // TODO: need confirmation dialog before proceeding
            $.post('/api/module/remove',
                {
                    id: module.id(),
                    text: ''
                },
                function (data, status, xhr) {
                    if (status === 'success') {
                        $('.module-editor').hide();
                        self.modules.remove(module);
                    } else {
                        alert('fail'); // TODO
                    }
                }, 'json');
        };
        return this;
    };

    PlexNewModuleDialogViewModel = function () {
        var self = this;
        /* props */
        this.moduleId = ko.observable();
        /* methods */
        this.showDialog = function () {
            $('#new-module-dialog').parent().children().hide();
            $('#new-module-dialog').show();
        };
        this.accept = function () {
            $.post('/api/module/upsert',
                {
                    id: self.moduleId(),
                    text: ''
                },
                function (data, status, xhr) {
                    if (status === 'success') {
                        self.onModuleUpsert(data);
                        $('#new-module-dialog').hide();
                    }
                }, 'json');
        };
        this.cancel = function () {
            $('#new-module-dialog').hide();
        };
        // raised on module upsert success
        this.onModuleUpsert = function (page) {
        };
        return this;
    };

    PlexMediaEditorViewModel = function () {
        var self = this;
        this.medias = ko.observableArray([]);
        this.selectedMedia = ko.observable();
        this.showView = function () {
            $('.media-editor').parent().children().hide();
            $('.media-editor').show();
        };
        this.mediaRemove = function (media) {
            // TODO: need confirmation dialog before proceeding
            $.post('/api/media/remove',
                ko.toJS(media),
                function (data, status, xhr) {
                    if (status === 'success') {
                        self.medias.remove(media);
                    } else {
                        alert('fail'); // TODO
                    }
                }, 'json');
        };
        return this;
    };

    PlexMediaUploadDialogViewModel = function () {
        var self = this;
        this.uploads = ko.observableArray([]);
        this.createUUID = function () {
            // http://www.ietf.org/rfc/rfc4122.txt
            // http://stackoverflow.com/questions/105034/how-to-create-a-guid-uuid-in-javascript
            var s = [];
            var hexDigits = "0123456789abcdef";
            for (var i = 0; i < 36; i++) {
                s[i] = hexDigits.substr(Math.floor(Math.random() * 0x10), 1);
            }
            s[14] = "4";  // bits 12-15 of the time_hi_and_version field to 0010
            s[19] = hexDigits.substr((s[19] & 0x3) | 0x8, 1);  // bits 6-7 of the clock_seq_hi_and_reserved to 01
            s[8] = s[13] = s[18] = s[23] = "-";

            var uuid = s.join("");
            return uuid;
        };
        this.showDialog = function () {
            $('#media-upload-dialog').parent().children().hide();
            $('#media-upload-dialog').show();
            $('#media-submit-container').children().hide();

            var upload = {
                id: ko.observable(self.createUUID()),
                state: ko.observable('pending'),
                progress: ko.observable(0),
                speed: ko.observable(0),
                filename: ko.observable(''),
            };

            $('#media-submit-container').append('<button id="' + upload.id() + '">Browse for File to Upload..</button>');
            $('#' + upload.id()).Upload({
                url: '/api/media/upload',
                name: upload.id(),
                updateInterval: 1000,
                onSelect: function () {
                    upload.state('sending');
                    console.log('sending: ' + upload.id());
                    if (this.file !== null && this.file !== undefined) {
                        $('#' + upload.id()).hide();
                        upload.filename(this.file.name);
                        this.sendFile();
                        self.uploads.push(upload);
                    }
                    self.showDialog(); // setup for another upload
                },
                onUpdate: function () {
                    upload.progress(this.getPercentage());
                    upload.speed(this.getSpeed());
                },
                onComplete: function (data) {
                    // process media info from server, update client state
                    for (var i = 0; i < data.length; i++) {
                        var media = {
                            folderName: ko.observable(data[i].folderName),
                            fileName: ko.observable(data[i].fileName),
                            length: ko.observable(data[i].length),
                        };
                        self.onMediaUpload(media);
                    }

                    $('#' + upload.id()).remove();
                    upload.state('complete');
                    // TODO: user will remove from self.uploads via click binding
                },
                onError: function (xhr) {
                    upload.state('error');
                },
                onCancel: function () {
                    upload.state('cancelled');
                }
            });
        };
        this.onMediaUpload = function (media) {
            // NOP - consumer should update any media lists it has access to
        };
        return this;
    };

    PlexHelpSystemViewModel = function() {
        var self = this;
        this.sections = ko.observableArray([]);
        // TODO: enumerate sections in div.help-system, will create nav options
        $('div.help-system > section').each(function (i, e) {
            self.sections.push({
                title: e.getAttribute('title')
            });
        });
        this.showSection = function (section) {
            $('div.help-system').parent().children().hide();
            $('div.help-system').children().hide();
            $('section[title="' + section.title + '"]').show();
            $('.help-system').show();
        };
        this.showSection({ title: "Welcome!" });
        return this;
    };

    PlexAdminViewModel = function () {
        var self = this;

        /* pages */
        this.pageEditor = new PlexPageEditorViewModel();
        this.newPageDialog = new PlexNewPageDialogViewModel();
        self.newPageDialog.onPageUpsert = function (data) {
            var page = {
                id: ko.observable(data.id),
                controllerId: ko.observable(data.controllerId),
                title: ko.observable(data.title),
                layoutId: ko.observable(data.layoutId),
                sections: ko.observableArray([]),
                body: ko.observable(data.body)
            };
            if (data.sections !== null) {
                for (var s = 0; s < data.sections.length; s++) {
                    var section = {
                        id: ko.observable(data.sections[s].id),
                        pageId: ko.observable(data.sections[s].pageId),
                        controllerId: ko.observable(data.sections[s].controllerId),
                        ordinal: ko.observable(data.sections[s].ordinal),
                        isPresent: ko.observable(data.sections[s].isPresent),
                        modules: ko.observableArray([])
                    };
                    if (data.sections[s].modules !== null) {
                        if (data.sections[s].modules) {
                            for (var m = 0; m < data.sections[s].modules.length; m++) {
                                section.modules.push({
                                    id: ko.observable(data.sections[s].modules[m].id),
                                    pageId: ko.observable(data.sections[s].modules[m].pageId),
                                    controllerId: ko.observable(data.sections[s].modules[m].controllerId),
                                    sectionId: ko.observable(data.sections[s].modules[m].sectionId),
                                    ordinal: ko.observable(data.sections[s].modules[m].ordinal)
                                });
                            }
                        }
                    }
                    page.sections.push(section);
                }
            }

            self.pageEditor.pages.push(page);
            self.pageEditor.controllers().filter(function (c) {
                if (c.id() === page.controllerId()) {
                    c.pages.push(page);
                }
                return false;
            });
            self.pageEditor.selectPage(page);
        };

        /* misc */
        this.helpSystem = new PlexHelpSystemViewModel();
        this.notImplemented = function () {
            alert('not implemented yet');
        };

        /* layouts */
        this.layoutEditor = new PlexLayoutEditorViewModel();
        this.defaultLayout = ko.observable();
        this.getDefaultLayout = function () {
            $.post('/api/layout/GetDefault',
                function (data, status, xhr) {
                    if (status === 'success') {
                        self.defaultLayout({ id: ko.observable(data.id) });
                    }
                }, 'json');
        };
        this.setDefaultLayout = function (layout) {
            $.post('/api/layout/SetDefault',
                { id: layout.id() },
                function (data, status, xhr) {
                    if (status === 'success') {
                        self.defaultLayout({ id: ko.observable(data.id) });
                    }
                });
        };

        /* modules */
        this.moduleEditor = new PlexModuleEditorViewModel();
        this.newModuleDialog = new PlexNewModuleDialogViewModel();
        self.newModuleDialog.onModuleUpsert = function (data) {
            var module = {
                id: ko.observable(data.id),
                text: ko.observable(data.text)
            };
            self.moduleEditor.modules.push(module);
            self.moduleEditor.selectModule(module);
        };

        /* media */
        this.mediaEditor = new PlexMediaEditorViewModel();
        this.mediaUploadDialog = new PlexMediaUploadDialogViewModel();
        self.mediaUploadDialog.onMediaUpload = function (media) {
            self.mediaEditor.medias.push(media);
        };

        /* loader */
        this.loadFromServer = function () {
            // load initial view model data from server
            var loaders_idx = 0;
            var loaders = [
                function() {
                    $.post('/api/layout/index',
                        function (data, status, xhr) {
                            if (status === 'success') {
                                for (var i = 0; i < data.length; i++) {
                                    var layout = data[i];
                                    var sections = ko.observableArray([]);
                                    for (var s = 0; s < layout.sections.length; s++) {
                                        var section = layout.sections[s];
                                        var modules = ko.observableArray([]);
                                        for (var m = 0; m < section.modules.length; m++) {
                                            var module = section.modules[m];
                                            modules.push({
                                                id: ko.observable(module.id),
                                                layoutId: ko.observable(module.layoutId),
                                                sectionId: ko.observable(module.sectionId),
                                                ordinal: ko.observable(module.ordinal)
                                            });
                                        }
                                        sections.push({
                                            id: ko.observable(section.id),
                                            layoutId: ko.observable(section.layoutId),
                                            ordinal: ko.observable(section.ordinal),
                                            modules: modules
                                        });
                                    }
                                    self.layoutEditor.layouts.push({
                                        id: ko.observable(layout.id),
                                        sections: sections
                                    });
                                }
                            }
                            self.pageEditor.layouts = self.layoutEditor.layouts;
                            loaders[loaders_idx++]();
                        }, 'json');
                },
                function () {
                    self.getDefaultLayout();
                    loaders[loaders_idx++]();
                },
                function () {
                    $.post('/api/controller/index',
                        function (data, status, xhr) {
                            if (status === 'success') {
                                for (var i = 0; i < data.length; i++) {
                                    var controller = data[i];
                                    self.pageEditor.controllers.push({
                                        id: ko.observable(controller.id),
                                        isPageController: ko.observable(controller.isPageController),
                                        pages: ko.observableArray([])
                                    });
                                }
                            }
                            loaders[loaders_idx++]();
                        }, 'json');
                },
                function () {
                    $.post('/api/page/index',
                        function (data, status, xhr) {
                            if (status === 'success') {
                                for (var i = 0; i < data.length; i++) {
                                    var page = {
                                        id: ko.observable(data[i].id),
                                        controllerId: ko.observable(data[i].controllerId),
                                        title: ko.observable(data[i].title),
                                        layoutId: ko.observable(data[i].layoutId),
                                        sections: ko.observableArray([]),
                                        body: ko.observable(data[i].body)
                                    };
                                    for (var s = 0; s < data[i].sections.length; s++) {
                                        var section = {
                                            id: ko.observable(data[i].sections[s].id),
                                            pageId: ko.observable(data[i].sections[s].pageId),
                                            controllerId: ko.observable(data[i].sections[s].controllerId),
                                            ordinal: ko.observable(data[i].sections[s].ordinal),
                                            isPresent: ko.observable(data[i].sections[s].isPresent),
                                            modules: ko.observableArray([])
                                        };
                                        if (data[i].sections[s].modules) {
                                            for (var m = 0; m < data[i].sections[s].modules.length; m++) {
                                                section.modules.push({
                                                    id: ko.observable(data[i].sections[s].modules[m].id),
                                                    pageId: ko.observable(data[i].sections[s].modules[m].pageId),
                                                    controllerId: ko.observable(data[i].sections[s].modules[m].controllerId),
                                                    sectionId: ko.observable(data[i].sections[s].modules[m].sectionId),
                                                    ordinal: ko.observable(data[i].sections[s].modules[m].ordinal)
                                                });
                                            }
                                        }
                                        page.sections.push(section);
                                    }
                                    self.pageEditor.pages.push(page);
                                }
                                for (var p = 0; p < self.pageEditor.pages().length; p++) {
                                    var page = self.pageEditor.pages()[p];
                                    for (var i = 0; i < self.pageEditor.controllers().length; i++) {
                                        var controller = self.pageEditor.controllers()[i];
                                        if (controller.id() === page.controllerId()) {
                                            controller.pages.push(page);
                                        }
                                    }
                                }
                            }
                            loaders[loaders_idx++]();
                        }, 'json');
                },
                function () {
                    $.post('/api/module/index',
                        function (data, status, xhr) {
                            if (status === 'success') {
                                for (var i = 0; i < data.length; i++) {
                                    var module = data[i];
                                    self.moduleEditor.modules.push({
                                        id: ko.observable(module.id),
                                        text: ko.observable(module.text)
                                    });
                                }
                            }
                            loaders[loaders_idx++]();
                        }, 'json');
                },
                function () {
                    $.post('/api/media/index',
                        function (data, status, xhr) {
                            if (status === 'success') {
                                for (var i = 0; i < data.length; i++) {
                                    var media = {
                                        folderName: ko.observable(data[i].folderName),
                                        fileName: ko.observable(data[i].fileName),
                                        length: ko.observable(data[i].length),
                                    };
                                    self.mediaEditor.medias.push(media);
                                }
                            }
                            loaders[loaders_idx++]();
                        }, 'json');
                },
                function () { /* NOP */ }];
            loaders[loaders_idx++]();
        }
        return this;
    };

    $(document).ready(function () {
        var vm = PlexAdminViewModel();
        ko.applyBindings(vm);
        vm.loadFromServer();
    });
})();
